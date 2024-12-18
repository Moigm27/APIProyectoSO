using APIProyectoSO.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;
using System.Threading;

namespace APIProyectoSO.Controladores
{
    /// <summary>
    /// Controlador para gestionar las operaciones de los usuarios, como login, registro y consulta.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Semáforo para controlar el acceso concurrente a las operaciones críticas.
        /// </summary>
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor que inicializa el controlador con el contexto de la base de datos.
        /// </summary>
        /// <param name="context">Contexto de la base de datos.</param>
        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para iniciar sesión de un usuario.
        /// </summary>
        /// <param name="loginRequest">Datos de inicio de sesión (correo y contraseña).</param>
        /// <returns>Devuelve el ID y nombre del usuario si las credenciales son válidas.</returns>
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                // Buscar el usuario por email y contraseña.
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == loginRequest.Email && u.contrasena == loginRequest.Contrasena);

                if (usuario == null)
                {
                    return Unauthorized("Credenciales inválidas.");
                }

                // Retornar el ID y nombre del usuario.
                return Ok(new { usuarioId = usuario.UsuarioID, nombre = usuario.Nombre });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al iniciar sesión.");
            }
        }

        /// <summary>
        /// Método para registrar un nuevo usuario y crear una cuenta predeterminada.
        /// </summary>
        /// <param name="request">Datos del nuevo usuario (nombre, correo y contraseña).</param>
        /// <returns>Mensaje de éxito y número de cuenta predeterminada.</returns>
        [HttpPost("RegistrarUsuario")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] RegistroRequest request)
        {
            // Validar los datos de entrada.
            if (string.IsNullOrEmpty(request.Nombre) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Contrasena))
            {
                return BadRequest("Todos los campos son obligatorios.");
            }

            // Exclusión mutua para evitar problemas de concurrencia.
            await _semaphore.WaitAsync();
            try
            {
                // Verificar si el usuario ya existe en la base de datos.
                var usuarioExistente = await _context.Usuarios
                    .AnyAsync(u => u.Email == request.Email);

                if (usuarioExistente)
                {
                    return Conflict("El usuario ya existe con este correo electrónico.");
                }

                // Crear un nuevo usuario.
                var nuevoUsuario = new Usuarios
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    FechaRegistro = DateTime.Now,
                    contrasena = request.Contrasena
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // Crear una cuenta predeterminada para el usuario registrado.
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

                // Retornar la información del usuario y la cuenta creada.
                return Ok(new
                {
                    Message = "Usuario registrado exitosamente con cuenta predeterminada.",
                    UsuarioID = nuevoUsuario.UsuarioID,
                    NumeroCuenta = cuentaPredeterminada.NumeroCuenta
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al registrar el usuario: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Método para obtener los datos de un usuario por su ID.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <returns>Objeto del usuario si se encuentra en la base de datos.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuarios>> GetUsuarioById(int id)
        {
            try
            {
                // Buscar el usuario por su ID.
                var usuario = await _context.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    return NotFound();
                }

                return usuario;
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al obtener el usuario.");
            }
        }

        /// <summary>
        /// Método privado para generar un número de cuenta aleatorio.
        /// </summary>
        /// <returns>Un número de cuenta generado aleatoriamente.</returns>
        private string GenerarNumeroCuenta()
        {
            var random = new Random();
            return random.Next(1000, 10001).ToString(); // Número entre 1000 y 10000.
        }
    }

    /// <summary>
    /// Clase para representar la solicitud de registro de un nuevo usuario.
    /// </summary>
    public class RegistroRequest
    {
        /// <summary>
        /// Nombre del usuario.
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Correo electrónico del usuario.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Contraseña del usuario.
        /// </summary>
        public string Contrasena { get; set; }

        /// <summary>
        /// Constructor para inicializar los valores por defecto.
        /// </summary>
        public RegistroRequest()
        {
            Nombre = "";
            Email = "";
            Contrasena = "";
        }
    }

    /// <summary>
    /// Clase para representar la solicitud de inicio de sesión.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Correo electrónico del usuario.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Contraseña del usuario.
        /// </summary>
        public string Contrasena { get; set; }

        /// <summary>
        /// Constructor que inicializa el correo y la contraseña.
        /// </summary>
        /// <param name="email">Correo electrónico.</param>
        /// <param name="contrasena">Contraseña.</param>
        public LoginRequest(string email, string contrasena)
        {
            Email = email;
            Contrasena = contrasena;
        }
    }
}
