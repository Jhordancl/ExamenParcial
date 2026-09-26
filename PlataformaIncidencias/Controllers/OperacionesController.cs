using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;

namespace PlataformaIncidencias.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(ApplicationDbContext db, ILogger<OperacionesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias()
    {
        var incidencias = await _db.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

        _logger.LogInformation("Listado obtenido desde base de datos: {Count} incidencias abiertas.", incidencias.Count);

        return View(incidencias);
    }

    // POST /Operaciones/Cerrar/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _db.Incidencias.FindAsync(id);
        if (incidencia == null)
        {
            _logger.LogWarning("Intento de cerrar incidencia inexistente Id={Id}", id);
            return NotFound();
        }

        incidencia.Estado = "Cerrada";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Incidencia Id={Id} cerrada correctamente.", id);

        return RedirectToAction(nameof(Incidencias));
    }
}
