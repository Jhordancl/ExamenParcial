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
    private readonly PieHostService _pieHost;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext db,
        PieHostService pieHost,
        ILogger<OperacionesController> logger)
    {
        _db = db;
        _pieHost = pieHost;
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

    // GET /Operaciones/ListadoJson
    // Permite al cliente reconectado consultar el estado vigente del listado sin recargar la página
    [HttpGet]
    public async Task<IActionResult> ListadoJson()
    {
        var incidencias = await _db.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaCreacion)
            .Select(i => new
            {
                i.Id,
                i.Estacion,
                i.Descripcion,
                i.Prioridad,
                i.Estado,
                FechaCreacion = i.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(incidencias);
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

        // 1. Primero persiste el cambio de estado en la base de datos
        incidencia.Estado = "Cerrada";
        await _db.SaveChangesAsync();
        _logger.LogInformation("Incidencia Id={Id} guardada con Estado='Cerrada' en Base de Datos.", id);

        // 2. SOLO DESPUÉS publica desde el servidor el evento IncidenciaActualizada al canal de PieHost
        await _pieHost.PublicarIncidenciaActualizadaAsync(id, "Cerrada");
        _logger.LogInformation("Evento IncidenciaActualizada publicado a PieHost tras persistencia en DB para Id={Id}.", id);

        return RedirectToAction(nameof(Incidencias));
    }
}
