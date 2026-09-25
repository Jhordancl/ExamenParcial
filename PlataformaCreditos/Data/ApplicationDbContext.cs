using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<SolicitudCredito> Solicitudes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Cliente>()
                .Property(c => c.IngresosMensuales)
                .HasColumnType("decimal(18,2)");

            builder.Entity<SolicitudCredito>()
                .Property(s => s.MontoSolicitado)
                .HasColumnType("decimal(18,2)");

            // Índice único filtrado: solo 1 solicitud "Pendiente" (Estado = 0) por Cliente
            builder.Entity<SolicitudCredito>()
                .HasIndex(s => s.ClienteId)
                .HasFilter("\"Estado\" = 0")
                .IsUnique();
        }
    }
}