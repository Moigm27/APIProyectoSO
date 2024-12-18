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
    /// <summary>
    /// Controlador para gestionar las operaciones relacionadas con cuentas bancarias,
    /// como transferencias y creación de cuentas de ahorro.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ctr_cuentas : ControllerBase
    {
        private readonly AppDbContext _context;

        // Semáforo para controlar la concurrencia en operaciones críticas.
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor que inicializa el controlador con el contexto de la base de datos.
        /// </summary>
        /// <param name="context">Contexto de la base de datos.</param>
        public ctr_cuentas(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las cuentas asociadas a un usuario específico.
        /// </summary>
        /// <param name="usuarioId">ID del usuario.</param>
        /// <returns>Una lista de cuentas asociadas al usuario.</returns>
        [HttpGet("{usuarioId}")]
        public async Task<ActionResult<IEnumerable<Cuentas>>> GetCuentasByUsuarioId(int usuarioId)
        {
            // Consulta las cuentas en la base de datos asociadas al ID del usuario.
            var cuentas = await _context.Cuentas
                                        .Where(c => c.UsuarioID == usuarioId)
                                        .ToListAsync();

            if (cuentas == null || cuentas.Count == 0)
            {
                return NotFound("No se encontraron cuentas para este Usuario");
            }

            return Ok(cuentas);
        }

        /// <summary>
        /// Realiza una transferencia de fondos entre dos cuentas.
        /// </summary>
        /// <param name="request">Objeto que contiene los datos de la transferencia.</param>
        /// <returns>Un mensaje indicando el resultado de la operación.</returns>
        [HttpPost("Transferir")]
        public async Task<IActionResult> TransferirFondos([FromBody] TransferenciaRequest request)
        {
            // Validación de los datos de entrada.
            if (request == null || string.IsNullOrEmpty(request.NumeroCuentaOrigen) ||
                string.IsNullOrEmpty(request.NumeroCuentaDestino) || request.Monto <= 0)
            {
                return BadRequest("Datos de la transferencia inválidos.");
            }

            // Controlar la concurrencia mediante un semáforo.
            await _semaphore.WaitAsync();

            try
            {
                // Ejecutar el procesamiento de la transferencia en un hilo separado.
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

        /// <summary>
        /// Procesa la transferencia de fondos entre dos cuentas.
        /// </summary>
        /// <param name="request">Datos de la solicitud de transferencia.</param>
        /// <returns>Una tupla con el resultado de la operación.</returns>
        private (bool Success, string Message, int? TransaccionID) ProcesarTransferencia(TransferenciaRequest request)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Obtener las cuentas origen y destino.
                var cuentaOrigen = _context.Cuentas.FirstOrDefault(c => c.NumeroCuenta == request.NumeroCuentaOrigen);
                var cuentaDestino = _context.Cuentas.FirstOrDefault(c => c.NumeroCuenta == request.NumeroCuentaDestino);

                // Validar que las cuentas existan.
                if (cuentaOrigen == null) return (false, "La cuenta de origen no existe.", null);
                if (cuentaDestino == null) return (false, "La cuenta de destino no existe.", null);

                // Realizar la transferencia.
                cuentaOrigen.Saldo -= request.Monto;
                cuentaDestino.Saldo += request.Monto;

                _context.Cuentas.Update(cuentaOrigen);
                _context.Cuentas.Update(cuentaDestino);

                Thread.Sleep(5000); // Simula un retraso en la transferencia.

                // Registrar la transacción.
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

        /// <summary>
        /// Crea una cuenta de ahorros para un usuario específico.
        /// </summary>
        /// <param name="request">Objeto que contiene el ID del usuario.</param>
        /// <returns>Información de la cuenta de ahorros creada.</returns>
        [HttpPost("CrearCuentaAhorros")]
        public async Task<IActionResult> CrearCuentaAhorros([FromBody] UsuarioRequest request)
        {
            // Validación de los datos de entrada.
            if (request == null || request.UsuarioId <= 0)
            {
                return BadRequest(new { Message = "Datos inválidos. El ID del usuario es requerido." });
            }

            try
            {
                // Verificar si el usuario existe.
                var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.UsuarioID == request.UsuarioId);
                if (!usuarioExiste)
                {
                    return NotFound(new { Message = "El usuario no existe." });
                }

                // Verificar si el usuario ya tiene una cuenta de ahorros.
                var tieneCuentaAhorros = await _context.Cuentas
                    .AnyAsync(c => c.UsuarioID == request.UsuarioId && c.TipoCuenta == "Ahorros");

                if (tieneCuentaAhorros)
                {
                    return BadRequest(new { Message = "El usuario ya tiene una cuenta de ahorros." });
                }

                // Crear la nueva cuenta de ahorros.
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

    /// <summary>
    /// Clase que representa la solicitud de transferencia.
    /// </summary>
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

    /// <summary>
    /// Clase que representa la solicitud de usuario para crear cuenta.
    /// </summary>
    public class UsuarioRequest
    {
        public int UsuarioId { get; set; }
    }
}
