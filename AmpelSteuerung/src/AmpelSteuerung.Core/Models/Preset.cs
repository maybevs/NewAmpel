namespace AmpelSteuerung.Core.Models;

/// <summary>
/// Preset definition loaded from YAML files.
/// Supports Standard (alternating groups) and Final (independent sides) modes.
/// </summary>
public class Preset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "standard"; // "standard" or "final"

    public PresetTimerSettings Timer { get; set; } = new();
    public PresetGroupSettings Groups { get; set; } = new();
    public PresetMatchSettings Match { get; set; } = new();
    public PresetOptions Options { get; set; } = new();
    public PresetFinalSettings? Final { get; set; }

    public bool IsFinalMode => Type.Equals("final", StringComparison.OrdinalIgnoreCase);
}

public class PresetTimerSettings
{
    public int ShootingTime { get; set; } = 120;
    public int PreparationTime { get; set; } = 10;
    public int WarningTime { get; set; } = 30;
}

public class PresetGroupSettings
{
    public string Mode { get; set; } = "alternating"; // "alternating" or "single"
    public string[] Names { get; set; } = ["AB", "CD"];
    public bool AlternateStartOrder { get; set; } = true;
}

public class PresetMatchSettings
{
    public int TotalEnds { get; set; } = 10;
    public int ArrowsPerEnd { get; set; } = 3;
}

public class PresetOptions
{
    public bool GroupSwitchEnabled { get; set; } = true;
    public bool SkipEnabled { get; set; } = true;
}

public class PresetFinalSettings
{
    public int ArrowsPerSide { get; set; } = 1;
    public int TotalArrowsPerEnd { get; set; } = 3;
    public string[] Sides { get; set; } = ["1", "2"];
    public string StartSide { get; set; } = "manual";
    public bool SkipEnabled { get; set; } = true;
}
