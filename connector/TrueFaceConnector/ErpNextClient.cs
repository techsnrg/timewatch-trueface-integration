using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace TrueFaceConnector;

public sealed class ErpNextClient
{
    private readonly HttpClient _httpClient;
    private readonly ConnectorOptions _options;

    public ErpNextClient(HttpClient httpClient, IOptions<ConnectorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.ErpNextBaseUrl.TrimEnd('/') + "/");
    }

    public async Task SendPunchesAsync(string deviceId, IReadOnlyList<PunchRecord> punches, CancellationToken cancellationToken)
    {
        var body = new
        {
            device_id = deviceId,
            api_token = _options.ApiToken,
            punches,
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "api/method/trueface_integration.api.attendance.receive_punches",
            body,
            cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"ERPNext rejected TrueFace punch batch: {(int)response.StatusCode} {content}");
        }
    }
}
