namespace AmpelSteuerung.Core.Configuration;

public class AmpelConfiguration
{
    public string ComPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public int DefaultDurationSeconds { get; set; } = 120;
    public int WarningTimeSeconds { get; set; } = 30;
    public bool SoundEnabled { get; set; } = true;
    public int SoundVolume { get; set; } = 100;
    public int ApiPort { get; set; } = 5000;
    public string LastGroup { get; set; } = "AB";
    public int LastTotalEnds { get; set; } = 10;
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 650;
    public string PresetsFile { get; set; } = "presets.json";
}
