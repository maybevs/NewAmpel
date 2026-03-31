using System.Text.Json.Serialization;

namespace AmpelSteuerung.StreamDeck.Models;

public class AmpelStateDto
{
    [JsonPropertyName("display1")]
    public DisplayStateDto Display1 { get; set; } = new();

    [JsonPropertyName("display2")]
    public DisplayStateDto Display2 { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "stopped";

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = "Idle";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "standard";

    [JsonPropertyName("currentEnd")]
    public string CurrentEnd { get; set; } = "1/10";

    [JsonPropertyName("currentSide")]
    public string CurrentSide { get; set; } = "left";

    [JsonPropertyName("arrowCountLeft")]
    public int ArrowCountLeft { get; set; }

    [JsonPropertyName("arrowCountRight")]
    public int ArrowCountRight { get; set; }

    // Derived helpers
    public bool IsRunning => Status == "running";
    public bool IsPaused => Status == "paused";
    public bool IsStopped => Status == "stopped";
    public bool IsFinalMode => Mode == "final";
    public bool IsEmergencyStopped => Phase == "EmergencyStopped";
    public bool IsEndCompleted => Phase == "EndCompleted";
    public bool IsIdle => Phase == "Idle";
    public bool IsShooting => Phase is "ShootingGroup1" or "ShootingGroup2";
    public bool IsPreparation => Phase is "PreparationGroup1" or "PreparationGroup2";
}

public class DisplayStateDto
{
    [JsonPropertyName("timeRemaining")]
    public int TimeRemaining { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; } = "";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "red";

    [JsonPropertyName("end")]
    public string End { get; set; } = "";
}

public class PresetDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "standard";

    [JsonPropertyName("shootingTime")]
    public int ShootingTime { get; set; }

    [JsonPropertyName("preparationTime")]
    public int PreparationTime { get; set; }

    [JsonPropertyName("totalEnds")]
    public int TotalEnds { get; set; }
}
