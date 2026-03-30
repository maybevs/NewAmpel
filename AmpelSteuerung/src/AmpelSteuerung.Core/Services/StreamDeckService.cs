using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

/// <summary>
/// Stub implementation — Stream Deck is controlled via REST API (Option B).
/// This class is a placeholder for a future native SDK integration (Option A).
/// </summary>
public class StreamDeckService : IStreamDeckService
{
    private readonly ILogger<StreamDeckService> _logger;

    public bool IsConnected => false;

    public StreamDeckService(ILogger<StreamDeckService> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("StreamDeck service initialized (REST-API mode — no native SDK)");
    }

    public void UpdateDisplay()
    {
        // No-op for REST API mode
    }

    public void Shutdown()
    {
        _logger.LogInformation("StreamDeck service shut down");
    }
}
