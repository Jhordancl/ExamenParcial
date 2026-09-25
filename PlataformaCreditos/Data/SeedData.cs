using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // Rol Analista
            if (!await roleManager.RoleExistsAsync("Analista"))
            {
                await roleManager.CreateAsync(new IdentityRole("Analista"));
            }

            // Usuario analista
            var analistaEmail = "analista@plataforma.com";
            var analista = await userManager.FindByEmailAsync(analistaEmail);
            if (analista is null)
            {
                analista = new IdentityUser
                {
                    UserName = analistaEmail,
                    Email = analistaEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(analista, "Analista123!");
                await userManager.AddToRoleAsync(analista, "Analista");
            }

            // Usuario cliente 1
            var cliente1Email = "cliente1@plataforma.com";
            var user1 = await userManager.FindByEmailAsync(cliente1Email);
            if (user1 is null)
            {
                user1 = new IdentityUser
                {
                    UserName = cliente1Email,
                    Email = cliente1Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user1, "Cliente123!");
            }

            // Usuario cliente 2
            var cliente2Email = "cliente2@plataforma.com";
            var user2 = await userManager.FindByEmailAsync(cliente2Email);
            if (user2 is null)
            {
                user2 = new IdentityUser
                {
                    UserName = cliente2Email,
                    Email = cliente2Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user2, "Cliente123!");
            }

            if (!context.Clientes.Any())
            {
                var cliente1 = new Cliente
                {
                    UsuarioId = user1.Id,
                    IngresosMensuales = 3000,
                    Activo = true
                };

                var cliente2 = new Cliente
                {
                    UsuarioId = user2.Id,
                    IngresosMensuales = 2000,
                    Activo = true
                };

                context.Clientes.AddRange(cliente1, cliente2);
                await context.SaveChangesAsync();

                context.Solicitudes.AddRange(
                    new SolicitudCredito
                    {
                        ClienteId = cliente1.Id,
                        MontoSolicitado = 5000,
                        FechaSolicitud = DateTime.UtcNow,
                        Estado = EstadoSolicitud.Pendiente
                    },
                    new SolicitudCredito
                    {
                        ClienteId = cliente2.Id,
                        MontoSolicitado = 4000,
                        FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                        Estado = EstadoSolicitud.Aprobado
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}