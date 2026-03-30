using System.Text.Json.Serialization;

namespace AmpelSteuerung.Core.Models;

public class Preset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ends")]
    public int Ends { get; set; } = 10;

    [JsonPropertyName("arrowsPerEnd")]
    public int ArrowsPerEnd { get; set; } = 3;

    [JsonPropertyName("groups")]
    public string[] Groups { get; set; } = ["AB", "CD"];

    [JsonPropertyName("timerDuration")]
    public int TimerDuration { get; set; } = 120;

    [JsonPropertyName("warningTime")]
    public int WarningTime { get; set; } = 30;

    [JsonPropertyName("sequence")]
    public PresetAction[] Sequence { get; set; } = [];
}

public class PresetAction
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("duration")]
    public int? Duration { get; set; }
}
