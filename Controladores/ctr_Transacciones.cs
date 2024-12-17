using APIProyectoSO.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIProyectoSO.Controladores
{
    [Route("api/[controller]")]
    [ApiController]
    public class ctr_Transacciones : ControllerBase
    {
        private readonly AppDbContext _context;

        public ctr_Transacciones(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("RegistrarTransaccion")]
        public async Task<IActionResult> RegistrarTransaccion([FromBody] Transacciones transaccion)
        {
            if (transaccion == null || transaccion.Monto <= 0)
            {
                return BadRequest("Datos inválidos para la transacción.");
            }

            try
            {
                // Agregar la transacción con estado inicial "En Proceso"
                transaccion.FechaTransaccion = DateTime.Now;
                transaccion.Estado = "En Proceso";

                _context.Transacciones.Add(transaccion);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Transacción registrada con éxito.", TransaccionID = transaccion.TransaccionID });
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message;
                return StatusCode(500, new
                {
                    Message = "Error al registrar la transacción.",
                    Error = ex.Message,
                    InnerError = innerException
                });
            }
        }
        /// <summary>
        /// Obtener todas las transacciones de un usuario.
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>Lista de transacciones</returns>
        [HttpGet("GetTransaccionesByUsuarioId/{usuarioId}")]
        public async Task<IActionResult> GetTransaccionesByUsuarioId(int usuarioId)
        {
            try
            {
                // Llamar al método separado para obtener las transacciones
                var transacciones = await ObtenerTransaccionesPorUsuarioId(usuarioId);

                if (transacciones == null || !transacciones.Any())
                {
                    return NotFound(new { Message = "No se encontraron transacciones para este usuario." });
                }

                return Ok(transacciones);
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message;
                return StatusCode(500, new
                {
                    Message = "Error al obtener las transacciones.",
                    Error = ex.Message,
                    InnerError = innerException
                });
            }
        }

        /// <summary>
        /// Método privado para obtener las transacciones de un usuario dado.
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>Lista de transacciones</returns>
        private async Task<List<Transacciones>> ObtenerTransaccionesPorUsuarioId(int usuarioId)
        {
            // Obtener las cuentas asociadas al usuario
            var cuentasUsuario = await _context.Cuentas
                .Where(c => c.UsuarioID == usuarioId)
                .Select(c => c.CuentaID)
                .ToListAsync();

            // Si no hay cuentas, retornar una lista vacía
            if (!cuentasUsuario.Any())
            {
                return new List<Transacciones>();
            }

            // Filtrar las transacciones donde las cuentas sean origen o destino
            var transacciones = await _context.Transacciones
                .Where(t => cuentasUsuario.Contains(t.CuentaOrigenID) || cuentasUsuario.Contains(t.CuentaDestinoID))
                .ToListAsync();

            return transacciones;
        }
    }

}


