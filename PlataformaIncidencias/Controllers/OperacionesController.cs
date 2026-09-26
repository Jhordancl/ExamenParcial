using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AlgoliaSearchService _algolia;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext db,
        AlgoliaSearchService algolia,
        ILogger<OperacionesController> logger)
    {
        _db = db;
        _algolia = algolia;
        _logger = logger;
    }

    // GET /Operaciones/Incidencias?q=texto
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Query = q;

        if (string.IsNullOrWhiteSpace(q))
        {
            // Sin búsqueda: listado habitual desde DB (sin pasar por Algolia)
            var todas = await _db.Incidencias
                .Where(i => i.Estado == "Abierta")
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            _logger.LogInformation("Listado general desde DB: {Count} incidencias abiertas.", todas.Count);
            return View(todas);
        }

        // Con búsqueda: consultar Algolia (server-side) y filtrar por Estado == "Abierta"
        _logger.LogInformation("Búsqueda Algolia con query='{Q}'", q);
        var ids = await _algolia.BuscarIdsAsync(q);

        var resultados = await _db.Incidencias
            .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

        _logger.LogInformation("Algolia+DB: {Count} incidencias abiertas para query='{Q}'", resultados.Count, q);
        return View(resultados);
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
