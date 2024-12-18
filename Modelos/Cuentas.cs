namespace APIProyectoSO.Modelos
{
    public class Cuentas
    {
        public int CuentaID { get; set; }  // ID único de la cuenta
        public int UsuarioID { get; set; } // ID del usuario propietario
        public string? NumeroCuenta { get; set; } // Número único de cuenta
        public decimal Saldo { get; set; } // Saldo de la cuenta
        public DateTime FechaCreacion { get; set; } // Fecha de creación
        public string TipoCuenta { get; set; } //Tipo de cuenta

        // Constructor por defecto
        public Cuentas()
        {
            FechaCreacion = DateTime.Now;
            Saldo = 0; // Por defecto el saldo es 0
            TipoCuenta = "";
        }

        // Constructor parametrizado
        public Cuentas(int cuentaID, int usuarioID, string numeroCuenta, decimal saldo, DateTime fechaCreacion, string TipoCuenta)
        {
            CuentaID = cuentaID;
            UsuarioID = usuarioID;
            NumeroCuenta = numeroCuenta;
            Saldo = saldo;
            FechaCreacion = fechaCreacion;
            this.TipoCuenta = TipoCuenta;
        }
        
    }

}
