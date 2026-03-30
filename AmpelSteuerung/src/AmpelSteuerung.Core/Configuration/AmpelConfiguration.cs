namespace AmpelSteuerung.Core.Configuration;

public class AmpelConfiguration
{
    // Serial / RS485
    public string ComPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public int BroadcastIntervalMs { get; set; } = 100;

    // Display mapping
    public string Display1Side { get; set; } = "left";   // "left" or "right"
    public string Display2Side { get; set; } = "right";

    // Sound
    public bool SoundEnabled { get; set; } = true;
    public int SoundVolume { get; set; } = 100;
    public string? CustomSoundFile { get; set; }

    // REST API
    public int ApiPort { get; set; } = 5000;
    public bool CorsEnabled { get; set; } = true;

    // Presets
    public string PresetsDirectory { get; set; } = "./Presets";
    public string? LastUsedPreset { get; set; }

    // Timer defaults
    public int DefaultShootingTime { get; set; } = 120;
    public int DefaultPreparationTime { get; set; } = 10;
    public int DefaultWarningTime { get; set; } = 30;

    // Last used state
    public string LastGroup { get; set; } = "AB";
    public int LastTotalEnds { get; set; } = 10;

    // Window
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public int BeamerMonitor { get; set; } = 1;
}
