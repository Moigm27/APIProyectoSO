using APIProyectoSO.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;
using System.Threading;

namespace APIProyectoSO.Controladores
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Exclusión mutua para las operaciones críticas

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Usuarios/Login (Iniciar sesión)
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == loginRequest.Email && u.contrasena == loginRequest.Contrasena);

                if (usuario == null)
                {
                    return Unauthorized("Credenciales inválidas.");
                }

                return Ok(new { usuarioId = usuario.UsuarioID, nombre = usuario.Nombre });
            }
            catch (Exception ex)
            {
                await RegistrarLog($"Error en Login: {ex.Message}", "Error");
                return StatusCode(500, "Error al iniciar sesión.");
            }
        }

        // POST: api/Usuarios/RegistrarUsuario (Registrar un nuevo usuario con cuenta predeterminada)
        [HttpPost("RegistrarUsuario")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] RegistroRequest request)
        {
            // Validar los datos de entrada
            if (string.IsNullOrEmpty(request.Nombre) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Contrasena))
            {
                return BadRequest("Todos los campos son obligatorios.");
            }

            // Exclusión mutua para evitar problemas de concurrencia en la creación de usuarios
            await _semaphore.WaitAsync();
            try
            {
                // Verificar si el usuario ya existe
                var usuarioExistente = await _context.Usuarios
                    .AnyAsync(u => u.Email == request.Email);

                if (usuarioExistente)
                {
                    return Conflict("El usuario ya existe con este correo electrónico.");
                }

                // Crear un nuevo usuario
                var nuevoUsuario = new Usuarios
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    FechaRegistro = DateTime.Now,
                    contrasena = request.Contrasena
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // Crear una cuenta predeterminada para el usuario
                var cuentaPredeterminada = new Cuentas
                {
                    UsuarioID = nuevoUsuario.UsuarioID,
                    NumeroCuenta = GenerarNumeroCuenta(),
                    Saldo = 0,
                    FechaCreacion = DateTime.Now,
                    TipoCuenta = "Corriente"
                };

                _context.Cuentas.Add(cuentaPredeterminada);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Usuario registrado exitosamente con cuenta predeterminada.",
                    UsuarioID = nuevoUsuario.UsuarioID,
                    NumeroCuenta = cuentaPredeterminada.NumeroCuenta
                });
            }
            catch (Exception ex)
            {
                await RegistrarLog($"Error en RegistrarUsuario: {ex.Message}", "Error");
                return StatusCode(500, $"Error al registrar el usuario: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // GET: api/Usuarios/{id} (Obtener un usuario por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuarios>> GetUsuarioById(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    return NotFound();
                }

                return usuario;
            }
            catch (Exception ex)
            {
                await RegistrarLog($"Error en GetUsuarioById: {ex.Message}", "Error");
                return StatusCode(500, "Error al obtener el usuario.");
            }
        }

        // Método privado para generar un número de cuenta aleatorio
        private string GenerarNumeroCuenta()
        {
            var random = new Random();
            return random.Next(1000, 10001).ToString(); // Número entre 1000 y 10000
        }

        // Método privado para registrar logs
        private async Task RegistrarLog(string mensaje, string tipo)
        {
            _context.LogIn.Add(new LogIn
            {
                Fecha = DateTime.Now,
                Mensaje = mensaje,
                Tipo = tipo
            });

            await _context.SaveChangesAsync();
        }
    }

    // Clase para la solicitud de registro
    public class RegistroRequest
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Contrasena { get; set; }

        public RegistroRequest()
        {
            Nombre = "";
            Email = "";
            Contrasena = "";
        }
    }

    // Clase para la solicitud de login
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Contrasena { get; set; }

        public LoginRequest(string email, string contrasena)
        {
            Email = email;
            Contrasena = contrasena;
        }
    }
}
