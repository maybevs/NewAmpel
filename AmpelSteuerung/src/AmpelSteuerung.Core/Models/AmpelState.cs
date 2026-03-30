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

/// <summary>
/// State machine phases for the end (Passe) workflow.
/// </summary>
public enum MatchPhase
{
    Idle,
    PreparationGroup1,
    ShootingGroup1,
    PreparationGroup2,
    ShootingGroup2,
    EndCompleted,
    EmergencyStopped
}

/// <summary>
/// Operating mode of the system.
/// </summary>
public enum OperatingMode
{
    Standard,
    Final
}

/// <summary>
/// State for a single display unit.
/// </summary>
public class DisplayState
{
    public int TimeRemaining { get; set; }
    public string Group { get; set; } = "AB";
    public AmpelColor Color { get; set; } = AmpelColor.Red;
    public string End { get; set; } = "1/10";

    public DisplayState Clone() => new()
    {
        TimeRemaining = TimeRemaining,
        Group = Group,
        Color = Color,
        End = End
    };

    public string ToSerialJson()
    {
        var c = Color switch
        {
            AmpelColor.Red => "R",
            AmpelColor.Green => "G",
            AmpelColor.Yellow => "Y",
            _ => "R"
        };
        return $"{{\"t\":{TimeRemaining},\"g\":\"{Group}\",\"c\":\"{c}\",\"e\":\"{End}\"}}";
    }
}

/// <summary>
/// Complete system state including both displays, phase information, and final mode data.
/// </summary>
public class AmpelState
{
    public DisplayState Display1 { get; set; } = new();
    public DisplayState Display2 { get; set; } = new();
    public TimerStatus Status { get; set; } = TimerStatus.Stopped;
    public MatchPhase Phase { get; set; } = MatchPhase.Idle;
    public OperatingMode Mode { get; set; } = OperatingMode.Standard;
    public bool ManualColorOverride { get; set; }

    // Standard mode convenience
    public int TimeRemaining => Display1.TimeRemaining;
    public string Group => Display1.Group;
    public AmpelColor Color => Display1.Color;
    public string End => Display1.End;

    // Final mode state
    public string CurrentSide { get; set; } = "left";
    public int ArrowCountLeft { get; set; }
    public int ArrowCountRight { get; set; }
    public string StartingSide { get; set; } = "left";

    // End tracking
    public int CurrentEnd { get; set; } = 1;
    public int TotalEnds { get; set; } = 10;

    public AmpelState Clone() => new()
    {
        Display1 = Display1.Clone(),
        Display2 = Display2.Clone(),
        Status = Status,
        Phase = Phase,
        Mode = Mode,
        ManualColorOverride = ManualColorOverride,
        CurrentSide = CurrentSide,
        ArrowCountLeft = ArrowCountLeft,
        ArrowCountRight = ArrowCountRight,
        StartingSide = StartingSide,
        CurrentEnd = CurrentEnd,
        TotalEnds = TotalEnds
    };

    /// <summary>
    /// Dual-display RS485 JSON: {"d1":{...},"d2":{...}}
    /// </summary>
    public string ToDualSerialJson()
    {
        return $"{{\"d1\":{Display1.ToSerialJson()},\"d2\":{Display2.ToSerialJson()}}}";
    }

    /// <summary>
    /// Set both displays to the same values (standard mode).
    /// </summary>
    public void SetBothDisplays(int time, string group, AmpelColor color, string end)
    {
        Display1.TimeRemaining = time;
        Display1.Group = group;
        Display1.Color = color;
        Display1.End = end;

        Display2.TimeRemaining = time;
        Display2.Group = group;
        Display2.Color = color;
        Display2.End = end;
    }
}
