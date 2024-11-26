namespace APIProyectoSO.Modelos
{
    public class LogIn
    {
        public int LogID { get; set; }     // ID único del log
        public DateTime Fecha { get; set; } // Fecha del evento
        public string? Mensaje { get; set; } // Descripción del evento o error
        public string? Tipo { get; set; }    // Tipo del log (Error, Info, etc.)

        // Constructor por defecto
        public LogIn()
        {
            Fecha = DateTime.Now;
        }

        // Constructor parametrizado
        public LogIn(int logID, DateTime fecha, string mensaje, string tipo)
        {
            LogID = logID;
            Fecha = fecha;
            Mensaje = mensaje;
            Tipo = tipo;
        }

        public override string ToString()
        {
            return $"LogID: {LogID}, Fecha: {Fecha}, Mensaje: {Mensaje}, Tipo: {Tipo}";
        }
    }
}
