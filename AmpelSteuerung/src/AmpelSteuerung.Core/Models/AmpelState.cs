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
/// How to format the timer display (seconds-only or minutes:seconds).
/// </summary>
public enum TimeDisplayFormat
{
    Minutes,    // M:SS or MM:SS
    Seconds,    // Raw seconds (e.g. 120) — default
    Finals      // SS:FF (seconds:centiseconds) — finals mode
}

/// <summary>
/// What to show on displays when the timer is stopped (idle).
/// </summary>
public enum IdleDisplayMode
{
    Off,        // Displays blank/red when timer stopped
    Clock,      // Show current time
    Message,    // Show scrolling/static message
    Both        // Message on top, clock on bottom
}

/// <summary>
/// State for a single display unit.
/// </summary>
public class DisplayState
{
    public int TimeRemaining { get; set; }
    public int TimeRemainingMs { get; set; }
    public string Group { get; set; } = "AB";
    public AmpelColor Color { get; set; } = AmpelColor.Red;
    public string End { get; set; } = "1/10";

    public DisplayState Clone() => new()
    {
        TimeRemaining = TimeRemaining,
        TimeRemainingMs = TimeRemainingMs,
        Group = Group,
        Color = Color,
        End = End
    };

    public string ToSerialJson(TimeDisplayFormat format = TimeDisplayFormat.Minutes)
    {
        var c = Color switch
        {
            AmpelColor.Red => "R",
            AmpelColor.Green => "G",
            AmpelColor.Yellow => "Y",
            _ => "R"
        };
        var f = format switch
        {
            TimeDisplayFormat.Seconds => "s",
            TimeDisplayFormat.Finals => "f",
            _ => "m"
        };
        // For finals format, send centiseconds; otherwise send whole seconds
        var t = format == TimeDisplayFormat.Finals
            ? TimeRemainingMs / 10
            : TimeRemaining;
        return $"{{\"t\":{t},\"g\":\"{Group}\",\"c\":\"{c}\",\"e\":\"{End}\",\"f\":\"{f}\"}}";
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

    // Timer format
    public TimeDisplayFormat TimeFormat { get; set; } = TimeDisplayFormat.Seconds;

    // Idle mode
    public IdleDisplayMode IdleMode { get; set; } = IdleDisplayMode.Clock;
    public string IdleMessage { get; set; } = "";
    public bool IdleMessageScroll { get; set; } = true;

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
        TotalEnds = TotalEnds,
        TimeFormat = TimeFormat,
        IdleMode = IdleMode,
        IdleMessage = IdleMessage,
        IdleMessageScroll = IdleMessageScroll
    };

    /// <summary>
    /// Dual-display RS485 JSON: {"d1":{...},"d2":{...}}
    /// </summary>
    public string ToDualSerialJson()
    {
        return $"{{\"d1\":{Display1.ToSerialJson(TimeFormat)},\"d2\":{Display2.ToSerialJson(TimeFormat)}}}";
    }

    /// <summary>
    /// Build idle-mode JSON for a single display.
    /// </summary>
    public static string ToIdleSerialJson(IdleDisplayMode mode, string message, bool scroll, string clock)
    {
        return mode switch
        {
            IdleDisplayMode.Clock =>
                $"{{\"c\":\"I\",\"idle\":{{\"mode\":\"clock\",\"clock\":\"{EscapeJson(clock)}\"}}}}",
            IdleDisplayMode.Message =>
                $"{{\"c\":\"I\",\"idle\":{{\"mode\":\"message\",\"text\":\"{EscapeJson(message)}\",\"scroll\":{(scroll ? "true" : "false")}}}}}",
            IdleDisplayMode.Both =>
                $"{{\"c\":\"I\",\"idle\":{{\"mode\":\"both\",\"text\":\"{EscapeJson(message)}\",\"scroll\":{(scroll ? "true" : "false")},\"clock\":\"{EscapeJson(clock)}\"}}}}",
            _ => "{\"c\":\"I\",\"idle\":{\"mode\":\"clock\",\"text\":\"\"}}"
        };
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

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
