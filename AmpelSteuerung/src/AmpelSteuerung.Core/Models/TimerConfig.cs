namespace AmpelSteuerung.Core.Models;

public class TimerConfig
{
    public int ShootingTimeSeconds { get; set; } = 120;
    public int PreparationTimeSeconds { get; set; } = 10;
    public int WarningTimeSeconds { get; set; } = 30;
    public int TotalEnds { get; set; } = 10;
    public int CurrentEnd { get; set; } = 1;
    public string[] Groups { get; set; } = ["AB", "CD"];
    public bool AlternateStartOrder { get; set; } = true;
    public bool SkipEnabled { get; set; } = true;
    public bool GroupSwitchEnabled { get; set; } = true;
}
