using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Soporte para puerto en Render ($PORT) ────────────────────────────────────
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// ── Base de datos SQLite + EF Core ──────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── ASP.NET Core Identity ────────────────────────────────────────────────────
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Simplificado para el examen
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PlataformaIncidencias.Services.PieHostService>();

// ── Redis Cache (IDistributedCache con StackExchange.Redis) ───────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "Incidencias_";
});

// ── Algolia (server-side, AdminKey nunca expuesta al cliente) ─────────────────
builder.Services.AddSingleton<PlataformaIncidencias.Services.AlgoliaSearchService>();

var app = builder.Build();

// ── Seed: migraciones + usuario supervisor ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Crear usuario supervisor si no existe
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    const string supervisorEmail = "supervisor@incidencias.com";
    const string supervisorPassword = "Supervisor1234!";

    if (await userManager.FindByEmailAsync(supervisorEmail) == null)
    {
        var supervisor = new IdentityUser
        {
            UserName = supervisorEmail,
            Email = supervisorEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(supervisor, supervisorPassword);
    }
}

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await app.RunAsync();
