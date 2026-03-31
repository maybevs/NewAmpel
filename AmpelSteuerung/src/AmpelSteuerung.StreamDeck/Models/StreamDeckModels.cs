using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmpelSteuerung.StreamDeck.Models;

public class StreamDeckEvent
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = "";

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [JsonPropertyName("device")]
    public string? Device { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }
}
