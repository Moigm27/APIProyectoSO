namespace APIProyectoSO.Modelos
{
    public class Transacciones
    {
        public int TransaccionID { get; set; }  // ID único de la transacción
        public int CuentaOrigenID { get; set; } // ID de la cuenta origen
        public int CuentaDestinoID { get; set; } // ID de la cuenta destino
        public decimal Monto { get; set; }      // Monto transferido
        public DateTime FechaTransaccion { get; set; } // Fecha de la transferencia
        public string Estado { get; set; }     // Estado de la transacción
        public string? Descripcion { get; set; }

        public Transacciones()
        {
            FechaTransaccion = DateTime.Now;
            Estado = "En Proceso"; // Estado inicial
        }

        // Constructor parametrizado
        public Transacciones(int transaccionID, int cuentaOrigenID, int cuentaDestinoID, decimal monto, DateTime fechaTransaccion, string estado, string descripcion)
        {
            TransaccionID = transaccionID;
            CuentaOrigenID = cuentaOrigenID;
            CuentaDestinoID = cuentaDestinoID;
            Monto = monto;
            FechaTransaccion = fechaTransaccion;
            Estado = estado;
            Descripcion = descripcion;
        }


    }
}
