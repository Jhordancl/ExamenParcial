using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incidencia> Incidencias { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Seed de incidencias de prueba
        builder.Entity<Incidencia>().HasData(
            new Incidencia { Id = 1, Estacion = "Estación Central", Descripcion = "Bicicleta con freno delantero roto", Prioridad = "Alta", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 10, 8, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 2, Estacion = "Estación Norte", Descripcion = "Candado bloqueado, no libera bicicleta", Prioridad = "Alta", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 3, Estacion = "Estación Sur", Descripcion = "Panel solar del dock sin carga", Prioridad = "Media", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 11, 10, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 4, Estacion = "Estación Este", Descripcion = "Rueda trasera pinchada en bicicleta #42", Prioridad = "Alta", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 11, 11, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 5, Estacion = "Estación Oeste", Descripcion = "Pantalla del kiosco no responde al tacto", Prioridad = "Media", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 12, 8, 30, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 6, Estacion = "Estación Central", Descripcion = "Sillín roto en bicicleta #17", Prioridad = "Baja", Estado = "Cerrada", FechaCreacion = new DateTime(2025, 1, 9, 15, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 7, Estacion = "Estación Norte", Descripcion = "Luz delantera no funciona en bicicleta #5", Prioridad = "Baja", Estado = "Cerrada", FechaCreacion = new DateTime(2025, 1, 9, 16, 0, 0, DateTimeKind.Utc) },
            new Incidencia { Id = 8, Estacion = "Estación Sur", Descripcion = "Conector de carga dañado en dock #3", Prioridad = "Media", Estado = "Abierta", FechaCreacion = new DateTime(2025, 1, 13, 9, 0, 0, DateTimeKind.Utc) }
        );
    }
}
