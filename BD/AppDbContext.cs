using APIProyectoSO.Modelos;
using Microsoft.EntityFrameworkCore; // Importa Entity Framework Core

namespace SistemaBancario.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor que pasa las opciones de configuración a la clase base DbContext
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        

        // DbSet para cada tabla de la base de datos
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Cuentas> Cuentas { get; set; }
        public DbSet<Transacciones> Transacciones { get; set; }
        public DbSet<LogIn> LogIn { get; set; }
        
        // Configuración adicional del modelo (opcional)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ejemplo: Configuración específica para una tabla o campo
            modelBuilder.Entity<Usuarios>()
                .HasKey(u => u.UsuarioID); // Clave primaria para la tabla Usuarios

            modelBuilder.Entity<Cuentas>()
                .HasKey(c => c.CuentaID); // Clave primaria para la tabla Cuentas

            modelBuilder.Entity<Transacciones>()
                .HasKey(t => t.TransaccionID); // Clave primaria para la tabla Transacciones

            modelBuilder.Entity<LogIn>()
                .HasKey(l => l.LogID); // Clave primaria para la tabla Logs

            // Relaciones (Foreign Keys)
            modelBuilder.Entity<Cuentas>()
                .HasOne<Usuarios>()
                .WithMany() // Un usuario puede tener muchas cuentas
                .HasForeignKey(c => c.UsuarioID);

            modelBuilder.Entity<Transacciones>()
                .HasOne<Cuentas>()
                .WithMany()
                .HasForeignKey(t => t.CuentaOrigenID)
                .OnDelete(DeleteBehavior.Restrict); // Evita la eliminación en cascada

            modelBuilder.Entity<Transacciones>()
                .HasOne<Cuentas>()
                .WithMany()
                .HasForeignKey(t => t.CuentaDestinoID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
