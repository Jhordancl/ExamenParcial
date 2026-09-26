using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace PlataformaIncidencias.Services;

/// <summary>
/// Servicio que publica eventos en tiempo real a PieHost WebSocket.
/// </summary>
public class PieHostService
{
    private readonly string? _channelUrl;
    private readonly string? _channelKey;
    private readonly ILogger<PieHostService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public PieHostService(IConfiguration config, ILogger<PieHostService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _channelUrl = config["PieHost:ChannelUrl"];
        _channelKey = config["PieHost:ChannelKey"];
    }

    /// <summary>
    /// Publica el evento IncidenciaActualizada a través de PieHost.
    /// Solo debe invocarse DESPUÉS de haber persistido el cambio en la base de datos.
    /// </summary>
    public async Task PublicarIncidenciaActualizadaAsync(int id, string estado)
    {
        var evento = new
        {
            @event = "IncidenciaActualizada",
            id = id,
            estado = estado,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(evento);

        if (string.IsNullOrWhiteSpace(_channelUrl) || _channelUrl.Contains("YOUR_PIEHOST"))
        {
            _logger.LogWarning("PieHost no configurado con URL válida. Evento simulado en logs: {Json}", json);
            return;
        }

        try
        {
            if (_channelUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
                _channelUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                // Conexión y envío por WebSocket client
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var ws = new ClientWebSocket();
                
                var urlConKey = string.IsNullOrWhiteSpace(_channelKey) 
                    ? _channelUrl 
                    : $"{_channelUrl}?apiKey={_channelKey}";

                await ws.ConnectAsync(new Uri(urlConKey), cts.Token);
                var buffer = Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Evento publicado", cts.Token);

                _logger.LogInformation("Evento IncidenciaActualizada publicado vía WebSocket a PieHost: Id={Id}, Estado={Estado}", id, estado);
            }
            else
            {
                // Envío por REST/HTTP API de PieHost
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(_channelKey))
                {
                    content.Headers.Add("apiKey", _channelKey);
                }

                var response = await client.PostAsync(_channelUrl, content);
                _logger.LogInformation("Evento IncidenciaActualizada publicado vía HTTP a PieHost. Status: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al publicar evento en PieHost para Incidencia Id={Id}", id);
        }
    }
}
