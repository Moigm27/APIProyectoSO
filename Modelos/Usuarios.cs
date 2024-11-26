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

        // Fecha de registro del usuario
        public DateTime FechaRegistro { get; set; }

        public Usuarios(int usuarioID, string nombre, string email, DateTime fechaRegistro)
        {
            UsuarioID = usuarioID;
            Nombre = nombre;
            Email = email;
            FechaRegistro = fechaRegistro;
        }
    }
}
