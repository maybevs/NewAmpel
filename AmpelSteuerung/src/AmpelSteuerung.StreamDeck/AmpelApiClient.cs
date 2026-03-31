using System.Net.Http.Json;
using System.Text.Json;
using AmpelSteuerung.StreamDeck.Models;

namespace AmpelSteuerung.StreamDeck;

public class AmpelApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string _baseUrl;

    public string BaseUrl
    {
        get => _baseUrl;
        set
        {
            _baseUrl = value.TrimEnd('/');
            _http.BaseAddress = new Uri(_baseUrl);
        }
    }

    public AmpelApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(3)
        };
    }

    public async Task<AmpelStateDto?> GetStateAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/state", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AmpelStateDto>(cancellationToken: ct);
    }

    public async Task<List<PresetDto>?> GetPresetsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/api/presets", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PresetDto>>(cancellationToken: ct);
        }
        catch { return null; }
    }

    public async Task<bool> PostCommandAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"/api/{endpoint}", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public void Dispose() => _http.Dispose();
}
