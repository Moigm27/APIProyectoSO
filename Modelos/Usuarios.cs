namespace APIProyectoSO.Modelos
{
    public class Usuarios
    {
        // ID único para cada usuario
        public int UsuarioID { get; set; }

        // Nombre completo del usuario
        public string Nombre { get; set; }

        // Email único del usuario
        public string Email { get; set; }

        // Contraseña del usuario
        public string contrasena { get; set; }

        // Fecha de registro del usuario
        public DateTime FechaRegistro { get; set; }

        // Constructor por defecto requerido por Entity Framework
        public Usuarios()
        {
        }

        // Constructor parametrizado
        public Usuarios(string nombre, string email, DateTime fechaRegistro, string contrasena)
        {
            Nombre = nombre;
            Email = email;
            FechaRegistro = fechaRegistro;
            this.contrasena = contrasena;
        }
    }
}
