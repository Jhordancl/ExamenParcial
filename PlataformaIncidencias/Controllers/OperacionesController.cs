using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly AlgoliaSearchService _algolia;
    private readonly PieHostService _pieHost;
    private readonly ILogger<OperacionesController> _logger;
    private const string CacheKeyListado = "incidencias_abiertas_listado";

    public OperacionesController(
        ApplicationDbContext db,
        IDistributedCache cache,
        AlgoliaSearchService algolia,
        PieHostService pieHost,
        ILogger<OperacionesController> logger)
    {
        _db = db;
        _cache = cache;
        _algolia = algolia;
        _pieHost = pieHost;
        _logger = logger;
    }

    // GET /Operaciones/Incidencias?q=texto
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Query = q;

        // La búsqueda por texto de Algolia NO debe pasar por esta caché, se consulta siempre en directo
        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("Búsqueda Algolia con query='{Q}'. Omitiendo caché de Redis y consultando en directo.", q);
            var ids = await _algolia.BuscarIdsAsync(q);

            var resultados = await _db.Incidencias
                .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            _logger.LogInformation("Algolia+DB (consulta directa sin caché): {Count} incidencias abiertas para query='{Q}'.", resultados.Count, q);
            return View(resultados);
        }

        // Listado general sin búsqueda: Cachear en Redis por 60 segundos
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
        _logger.LogInformation("Incidencia Id={Id} guardada como 'Cerrada' en BASE DE DATOS.", id);

        // 2. Invalidar (eliminar) la clave de caché de Redis ANTES de consultar el listado
        try
        {
            await _cache.RemoveAsync(CacheKeyListado);
            _logger.LogInformation("[CACHE INVALIDADA] Clave '{Key}' eliminada de REDIS tras cambio de estado.", CacheKeyListado);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo invalidar la clave de Redis tras cerrar incidencia Id={Id}.", id);
        }

        // 3. SOLO DESPUÉS publica desde el servidor el evento IncidenciaActualizada al canal de PieHost
        await _pieHost.PublicarIncidenciaActualizadaAsync(id, "Cerrada");
        _logger.LogInformation("Evento IncidenciaActualizada publicado a PieHost tras persistencia en DB para Id={Id}.", id);

        return RedirectToAction(nameof(Incidencias));
    }
}
