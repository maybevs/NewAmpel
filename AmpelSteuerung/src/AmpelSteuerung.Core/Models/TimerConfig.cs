namespace AmpelSteuerung.Core.Models;

public class TimerConfig
{
    public int DurationSeconds { get; set; } = 120;
    public int WarningTimeSeconds { get; set; } = 30;
    public int TotalEnds { get; set; } = 10;
    public int CurrentEnd { get; set; } = 1;
    public string[] Groups { get; set; } = ["AB", "CD"];
}
