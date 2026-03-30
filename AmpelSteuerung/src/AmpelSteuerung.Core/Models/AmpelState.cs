namespace AmpelSteuerung.Core.Models;

public enum AmpelColor
{
    Red,
    Green,
    Yellow
}

public enum TimerStatus
{
    Stopped,
    Running,
    Paused
}

public class AmpelState
{
    public int TimeRemaining { get; set; }
    public string Group { get; set; } = "AB";
    public AmpelColor Color { get; set; } = AmpelColor.Red;
    public string End { get; set; } = "1/10";
    public TimerStatus Status { get; set; } = TimerStatus.Stopped;
    public bool ManualColorOverride { get; set; }

    public AmpelState Clone()
    {
        return new AmpelState
        {
            TimeRemaining = TimeRemaining,
            Group = Group,
            Color = Color,
            End = End,
            Status = Status,
            ManualColorOverride = ManualColorOverride
        };
    }

    public string ToSerialJson()
    {
        var colorCode = Color switch
        {
            AmpelColor.Red => "R",
            AmpelColor.Green => "G",
            AmpelColor.Yellow => "Y",
            _ => "R"
        };
        return $"{{\"t\":{TimeRemaining},\"g\":\"{Group}\",\"c\":\"{colorCode}\",\"e\":\"{End}\"}}";
    }
}
