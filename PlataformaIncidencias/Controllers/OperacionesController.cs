using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OperacionesController> _logger;
    private const string CacheKeyListado = "incidencias_abiertas_listado";

    public OperacionesController(
        ApplicationDbContext db,
        IDistributedCache cache,
        ILogger<OperacionesController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    // GET /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Query = q;

        // La búsqueda por texto NO pasa por caché, se consulta siempre en directo
        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("Búsqueda con query='{Q}' solicitada. Omitiendo caché de Redis y consultando en directo.", q);
            var resultados = await _db.Incidencias
                .Where(i => i.Estado == "Abierta" && (i.Estacion.Contains(q) || i.Descripcion.Contains(q)))
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            _logger.LogInformation("Lectura realizada desde BASE DE DATOS (búsqueda directa): {Count} resultados.", resultados.Count);
            return View(resultados);
        }

        // Listado general: Intentar leer desde Redis (caché por 60 segundos)
        List<Incidencia>? incidencias = null;

        try
        {
            var cachedData = await _cache.GetStringAsync(CacheKeyListado);
            if (!string.IsNullOrEmpty(cachedData))
            {
                incidencias = JsonSerializer.Deserialize<List<Incidencia>>(cachedData);
                _logger.LogInformation("[REDIS HIT] Lectura realizada exitosamente desde REDIS. {Count} incidencias recuperadas de caché.", incidencias?.Count ?? 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al consultar caché de Redis. Se continuará con lectura desde base de datos.");
        }

        if (incidencias == null)
        {
            // Cache miss o fallo de Redis: consultar DB
            incidencias = await _db.Incidencias
                .Where(i => i.Estado == "Abierta")
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            _logger.LogInformation("[DATABASE HIT] Lectura realizada desde BASE DE DATOS. {Count} incidencias abiertas obtenidas.", incidencias.Count);

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                };
                var serialized = JsonSerializer.Serialize(incidencias);
                await _cache.SetStringAsync(CacheKeyListado, serialized, cacheOptions);
                _logger.LogInformation("Listado almacenado en REDIS con clave '{Key}' y TTL de 60 segundos.", CacheKeyListado);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar el listado en caché de Redis.");
            }
        }

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
        _logger.LogInformation("Incidencia Id={Id} guardada como 'Cerrada' en BASE DE DATOS.", id);

        // Invalidar (eliminar) la clave de caché ANTES de que se vuelva a consultar el listado
        try
        {
            await _cache.RemoveAsync(CacheKeyListado);
            _logger.LogInformation("[CACHE INVALIDADA] Clave '{Key}' eliminada de REDIS tras cambio de estado.", CacheKeyListado);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo invalidar la clave de Redis tras cerrar incidencia Id={Id}.", id);
        }

        return RedirectToAction(nameof(Incidencias));
    }
}
