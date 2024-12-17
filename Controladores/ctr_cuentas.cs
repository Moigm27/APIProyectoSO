using APIProyectoSO.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIProyectoSO.Controladores
{
    [Route("api/[controller]")]
    [ApiController]
    public class ctr_cuentas : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Limitar acceso concurrente

        public ctr_cuentas(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Cuentas/Usuario/{usuarioId} (Obtener todas las cuentas de un usuario específico)
        [HttpGet("{usuarioId}")]
        public async Task<ActionResult<IEnumerable<Cuentas>>> GetCuentasByUsuarioId(int usuarioId)
        {
            var cuentas = await _context.Cuentas
                                        .Where(c => c.UsuarioID == usuarioId)
                                        .ToListAsync();

            if (cuentas == null || cuentas.Count == 0)
            {
                return NotFound("No se encontraron cuentas para este Usuario");
            }

            return Ok(cuentas);
        }

        // POST: api/ctr_cuentas/Transferir
        [HttpPost("Transferir")]
        public async Task<IActionResult> TransferirFondos([FromBody] TransferenciaRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.NumeroCuentaOrigen) ||
                string.IsNullOrEmpty(request.NumeroCuentaDestino) || request.Monto <= 0)
            {
                return BadRequest("Datos de la transferencia inválidos.");
            }

            await _semaphore.WaitAsync(); // Controlar la concurrencia

            try
            {
                var result = await Task.Run(() => ProcesarTransferencia(request));

                if (!result.Success)
                {
                    return StatusCode(500, new { Message = result.Message });
                }

                return Ok(new { Message = "Transferencia realizada con éxito.", TransaccionID = result.TransaccionID });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private (bool Success, string Message, int? TransaccionID) ProcesarTransferencia(TransferenciaRequest request)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var cuentaOrigen = _context.Cuentas.FirstOrDefault(c => c.NumeroCuenta == request.NumeroCuentaOrigen);
                var cuentaDestino = _context.Cuentas.FirstOrDefault(c => c.NumeroCuenta == request.NumeroCuentaDestino);

                if (cuentaOrigen == null) return (false, "La cuenta de origen no existe.", null);
                if (cuentaDestino == null) return (false, "La cuenta de destino no existe.", null);

                cuentaOrigen.Saldo -= request.Monto;
                cuentaDestino.Saldo += request.Monto;

                _context.Cuentas.Update(cuentaOrigen);
                _context.Cuentas.Update(cuentaDestino);
                Thread.Sleep(5000);

                var transaccion = new Transacciones
                {
                    CuentaOrigenID = cuentaOrigen.CuentaID,
                    CuentaDestinoID = cuentaDestino.CuentaID,
                    Monto = request.Monto,
                    FechaTransaccion = DateTime.Now,
                    Estado = "Completada",
                    Descripcion = request.Descripcion ?? "Transferencia entre cuentas",
                    numeroCuentaDestino = request.NumeroCuentaDestino,
                    numeroCuentaOrigen = request.NumeroCuentaOrigen
                };

                _context.Transacciones.Add(transaccion);
                _context.SaveChanges();
                transaction.Commit();

                return (true, "Transferencia exitosa.", transaccion.TransaccionID);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return (false, $"Error al realizar la transferencia: {ex.Message}", null);
            }
        }

        // POST: api/ctr_cuentas/CrearCuentaAhorros
        [HttpPost("CrearCuentaAhorros")]
        public async Task<IActionResult> CrearCuentaAhorros([FromBody] UsuarioRequest request)
        {
            if (request == null || request.UsuarioId <= 0)
            {
                return BadRequest(new { Message = "Datos inválidos. El ID del usuario es requerido." });
            }

            try
            {
                // Verificar si el usuario existe
                var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.UsuarioID == request.UsuarioId);
                if (!usuarioExiste)
                {
                    return NotFound(new { Message = "El usuario no existe." });
                }

                // Verificar si el usuario ya tiene una cuenta de ahorros
                var tieneCuentaAhorros = await _context.Cuentas
                    .AnyAsync(c => c.UsuarioID == request.UsuarioId && c.TipoCuenta == "Ahorros");

                if (tieneCuentaAhorros)
                {
                    return BadRequest(new { Message = "El usuario ya tiene una cuenta de ahorros." });
                }

                // Crear la nueva cuenta de ahorros
                var nuevaCuenta = new Cuentas
                {
                    UsuarioID = request.UsuarioId,
                    NumeroCuenta = new Random().Next(1000, 10000).ToString(),
                    Saldo = 0,
                    FechaCreacion = DateTime.Now,
                    TipoCuenta = "Ahorros"
                };

                _context.Cuentas.Add(nuevaCuenta);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Cuenta de ahorros creada con éxito.",
                    NumeroCuenta = nuevaCuenta.NumeroCuenta,
                    Saldo = nuevaCuenta.Saldo,
                    FechaCreacion = nuevaCuenta.FechaCreacion,
                    TipoCuenta = nuevaCuenta.TipoCuenta
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error al crear la cuenta de ahorros.", Error = ex.Message });
            }
        }
    }

    public class TransferenciaRequest
    {
        public string NumeroCuentaOrigen { get; set; }
        public string NumeroCuentaDestino { get; set; }
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }

        public TransferenciaRequest()
        {
            NumeroCuentaOrigen = "";
            NumeroCuentaDestino = "";
        }
    }

    public class UsuarioRequest
    {
        public int UsuarioId { get; set; }
    }
}
