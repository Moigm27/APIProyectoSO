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
    public class ctr_LogIn : ControllerBase
    {
        private readonly AppDbContext _context;

        public ctr_LogIn(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LogIn (Obtener todos los logs)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LogIn>>> GetLogs()
        {
            return await _context.LogIn.ToListAsync();
        }

        // GET: api/LogIn/{id} (Obtener un log por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<LogIn>> GetLogById(int id)
        {
            var log = await _context.LogIn.FindAsync(id);

            if (log == null)
            {
                return NotFound();
            }

            return log;
        }

        // POST: api/LogIn (Crear un nuevo log)
        [HttpPost]
        public async Task<ActionResult<LogIn>> PostLog([FromBody] LogIn log)
        {
            if (log == null)
            {
                return BadRequest("El objeto Log no puede ser nulo.");
            }

            _context.LogIn.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLogById), new { id = log.LogID }, log);
        }

        // PUT: api/LogIn/{id} (Actualizar un log existente)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLog(int id, [FromBody] LogIn log)
        {
            if (id != log.LogID)
            {
                return BadRequest("El ID del log no coincide.");
            }

            _context.Entry(log).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LogExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/LogIn/{id} (Eliminar un log)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            var log = await _context.LogIn.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            _context.LogIn.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Método privado para verificar si un log existe
        private bool LogExists(int id)
        {
            return _context.LogIn.Any(e => e.LogID == id);
        }
    }
}
