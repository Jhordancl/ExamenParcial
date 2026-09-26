using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

/// <summary>
/// Servicio server-side que consulta Algolia para buscar incidencias.
/// La AdminKey NUNCA se expone al cliente ni al navegador.
/// </summary>
public class AlgoliaSearchService
{
    private readonly SearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<AlgoliaSearchService> _logger;

    public AlgoliaSearchService(IConfiguration config, ILogger<AlgoliaSearchService> logger)
    {
        _logger = logger;
        var appId = config["Algolia:AppId"]
            ?? throw new InvalidOperationException("Algolia:AppId no configurado.");
        var adminKey = config["Algolia:AdminKey"]
            ?? throw new InvalidOperationException("Algolia:AdminKey no configurado.");
        _indexName = config["Algolia:IndexName"] ?? "incidencias";

        _client = new SearchClient(appId, adminKey);
    }

    /// <summary>
    /// Busca en Algolia por texto libre (estación o descripción).
    /// Devuelve los IDs de incidencias que coinciden.
    /// </summary>
    public async Task<List<int>> BuscarIdsAsync(string query)
    {
        try
        {
            var request = new SearchMethodParams
            {
                Requests = new List<SearchQuery>
                {
                    new SearchQuery(new SearchForHits
                    {
                        IndexName = _indexName,
                        Query = query,
                        AttributesToRetrieve = new List<string> { "objectID" },
                        HitsPerPage = 100
                    })
                }
            };

            var response = await _client.SearchAsync<IncidenciaAlgoliaRecord>(request);
            var hits = response.Results[0].AsSearchResponse().Hits;

            var ids = hits
                .Where(h => int.TryParse(h.ObjectID, out _))
                .Select(h => int.Parse(h.ObjectID!))
                .ToList();

            _logger.LogInformation("Algolia devolvió {Count} resultados para query='{Query}'", ids.Count, query);
            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar Algolia para query='{Query}'", query);
            return new List<int>();
        }
    }

    /// <summary>
    /// Indexa (o re-indexa) todas las incidencias en Algolia.
    /// Se llama desde el seed inicial o desde una tarea de administración.
    /// </summary>
    public async Task IndexarIncidenciasAsync(List<Incidencia> incidencias)
    {
        var records = incidencias.Select(i => new IncidenciaAlgoliaRecord
        {
            ObjectID = i.Id.ToString(),
            Estacion = i.Estacion,
            Descripcion = i.Descripcion,
            Prioridad = i.Prioridad,
            Estado = i.Estado
        }).ToList();

        await _client.SaveObjectsAsync(_indexName, records);
        _logger.LogInformation("Se indexaron {Count} incidencias en Algolia.", records.Count);
    }
}

/// <summary>Registro plano para serializar/deserializar desde Algolia.</summary>
public class IncidenciaAlgoliaRecord
{
    public string? ObjectID { get; set; }
    public string Estacion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
