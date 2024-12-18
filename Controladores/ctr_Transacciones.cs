using APIProyectoSO.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIProyectoSO.Controladores
{
    /// <summary>
    /// Controlador para manejar las transacciones en el sistema bancario.
    /// Proporciona métodos para registrar y obtener transacciones.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ctr_Transacciones : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor para inicializar el controlador con el contexto de base de datos.
        /// </summary>
        /// <param name="context">Contexto de la base de datos.</param>
        public ctr_Transacciones(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra una nueva transacción en la base de datos.
        /// </summary>
        /// <param name="transaccion">Objeto de tipo Transacciones que contiene los datos de la transacción.</param>
        /// <returns>Un mensaje indicando el estado del registro de la transacción.</returns>
        [HttpPost("RegistrarTransaccion")]
        public async Task<IActionResult> RegistrarTransaccion([FromBody] Transacciones transaccion)
        {
            // Validar si los datos de la transacción son inválidos
            if (transaccion == null || transaccion.Monto <= 0)
            {
                return BadRequest("Datos inválidos para la transacción.");
            }

            try
            {
                // Agregar la transacción con estado inicial "En Proceso"
                transaccion.FechaTransaccion = DateTime.Now;
                transaccion.Estado = "En Proceso";

                // Insertar la transacción en la base de datos
                _context.Transacciones.Add(transaccion);
                await _context.SaveChangesAsync();

                // Retornar confirmación de éxito
                return Ok(new { Message = "Transacción registrada con éxito.", TransaccionID = transaccion.TransaccionID });
            }
            catch (Exception ex)
            {
                // Manejo de errores, incluye un mensaje de error interno si existe
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
        /// Obtiene todas las transacciones relacionadas con un usuario específico.
        /// </summary>
        /// <param name="usuarioId">ID del usuario del cual se desean obtener las transacciones.</param>
        /// <returns>Una lista de transacciones asociadas al usuario.</returns>
        [HttpGet("GetTransaccionesByUsuarioId/{usuarioId}")]
        public async Task<IActionResult> GetTransaccionesByUsuarioId(int usuarioId)
        {
            try
            {
                // Llamar al método privado para obtener las transacciones del usuario
                var transacciones = await ObtenerTransaccionesPorUsuarioId(usuarioId);

                // Verificar si no se encontraron transacciones
                if (transacciones == null || !transacciones.Any())
                {
                    return NotFound(new { Message = "No se encontraron transacciones para este usuario." });
                }

                // Retornar la lista de transacciones encontradas
                return Ok(transacciones);
            }
            catch (Exception ex)
            {
                // Manejo de errores, incluye un mensaje de error interno si existe
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
        /// Método privado que obtiene las transacciones de un usuario específico
        /// buscando todas las cuentas asociadas al usuario.
        /// </summary>
        /// <param name="usuarioId">ID del usuario del cual se desean obtener las transacciones.</param>
        /// <returns>Lista de transacciones asociadas al usuario.</returns>
        private async Task<List<Transacciones>> ObtenerTransaccionesPorUsuarioId(int usuarioId)
        {
            // Obtener las cuentas que pertenecen al usuario
            var cuentasUsuario = await _context.Cuentas
                .Where(c => c.UsuarioID == usuarioId)
                .Select(c => c.CuentaID)
                .ToListAsync();

            // Si no se encontraron cuentas, retornar una lista vacía
            if (!cuentasUsuario.Any())
            {
                return new List<Transacciones>();
            }

            // Obtener las transacciones donde las cuentas son origen o destino
            var transacciones = await _context.Transacciones
                .Where(t => cuentasUsuario.Contains(t.CuentaOrigenID) || cuentasUsuario.Contains(t.CuentaDestinoID))
                .ToListAsync();

            // Retornar la lista de transacciones
            return transacciones;
        }
    }
}
