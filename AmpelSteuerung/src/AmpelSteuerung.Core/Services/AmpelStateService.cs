using System.Diagnostics;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Models;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class AmpelStateService : IAmpelStateService, IDisposable
{
    private readonly object _lock = new();
    private readonly AmpelState _state = new();
    private readonly TimerConfig _config = new();
    private readonly ISoundService _soundService;
    private readonly ILogger<AmpelStateService> _logger;
    private readonly System.Timers.Timer _timer;
    private readonly Stopwatch _stopwatch = new();

    private int _countdownDuration; // current phase countdown total
    private int _startingGroupIndex; // 0 = first group starts, toggles per passe
    private int _emergencyFrozenTime; // time remaining when emergency was triggered

    // Final mode
    private Preset? _activePreset;
    private int _currentRound; // current round within an end (final mode)

    public string Display1Side { get; set; } = "left";
    public string Display2Side { get; set; } = "right";

    public AmpelState CurrentState
    {
        get { lock (_lock) return _state.Clone(); }
    }

    public TimerConfig Config
    {
        get
        {
            lock (_lock)
            {
                return new TimerConfig
                {
                    ShootingTimeSeconds = _config.ShootingTimeSeconds,
                    PreparationTimeSeconds = _config.PreparationTimeSeconds,
                    WarningTimeSeconds = _config.WarningTimeSeconds,
                    TotalEnds = _config.TotalEnds,
                    CurrentEnd = _config.CurrentEnd,
                    Groups = (string[])_config.Groups.Clone(),
                    AlternateStartOrder = _config.AlternateStartOrder,
                    SkipEnabled = _config.SkipEnabled,
                    GroupSwitchEnabled = _config.GroupSwitchEnabled
                };
            }
        }
    }

    public event EventHandler<AmpelState>? StateChanged;

    public AmpelStateService(ISoundService soundService, ILogger<AmpelStateService> logger, AmpelConfiguration configuration)
    {
        _soundService = soundService;
        _logger = logger;

        _config.ShootingTimeSeconds = configuration.DefaultShootingTime;
        _config.PreparationTimeSeconds = configuration.DefaultPreparationTime;
        _config.WarningTimeSeconds = configuration.DefaultWarningTime;
        _config.TotalEnds = configuration.LastTotalEnds;
        _config.CurrentEnd = 1;

        Display1Side = configuration.Display1Side;
        Display2Side = configuration.Display2Side;

        var endStr = $"1/{_config.TotalEnds}";
        _state.SetBothDisplays(0, configuration.LastGroup, AmpelColor.Red, endStr);
        _state.CurrentEnd = 1;
        _state.TotalEnds = _config.TotalEnds;

        _timer = new System.Timers.Timer(50);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Running) return;

            var elapsed = (int)_stopwatch.Elapsed.TotalSeconds;
            var newTime = Math.Max(0, _countdownDuration - elapsed);

            if (_state.Mode == OperatingMode.Standard)
                HandleStandardTick(newTime);
            else
                HandleFinalTick(newTime);
        }

        RaiseStateChanged();
    }

    private void HandleStandardTick(int newTime)
    {
        var currentTime = _state.Display1.TimeRemaining;
        if (newTime == currentTime) return;

        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        switch (_state.Phase)
        {
            case MatchPhase.PreparationGroup1:
            case MatchPhase.PreparationGroup2:
                _state.SetBothDisplays(newTime, _state.Display1.Group, AmpelColor.Red, endStr);
                if (newTime == 0)
                    TransitionToShooting();
                break;

            case MatchPhase.ShootingGroup1:
            case MatchPhase.ShootingGroup2:
                var color = AmpelColor.Green;
                if (!_state.ManualColorOverride && newTime <= _config.WarningTimeSeconds)
                    color = AmpelColor.Yellow;
                _state.SetBothDisplays(newTime, _state.Display1.Group, color, endStr);
                if (newTime == 0)
                    TransitionAfterShooting();
                break;
        }
    }

    private void HandleFinalTick(int newTime)
    {
        var currentTime = GetActiveDisplay().TimeRemaining;
        if (newTime == currentTime) return;

        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        switch (_state.Phase)
        {
            case MatchPhase.PreparationGroup1: // Preparation for both sides
                SetBothFinalDisplays(newTime, AmpelColor.Red, endStr);
                if (newTime == 0)
                    TransitionToFinalShooting();
                break;

            case MatchPhase.ShootingGroup1: // Active side shooting
            case MatchPhase.ShootingGroup2:
                var color = AmpelColor.Green;
                if (!_state.ManualColorOverride && newTime <= _config.WarningTimeSeconds)
                    color = AmpelColor.Yellow;

                var (activeDisp, inactiveDisp) = GetActiveSideDisplays();
                activeDisp.TimeRemaining = newTime;
                activeDisp.Color = color;
                activeDisp.End = endStr;
                inactiveDisp.TimeRemaining = 0;
                inactiveDisp.Color = AmpelColor.Red;
                inactiveDisp.End = endStr;

                if (newTime == 0)
                    TransitionFinalAfterShooting();
                break;
        }
    }

    private void TransitionToShooting()
    {
        _timer.Stop();
        _stopwatch.Reset();

        // 1× horn — shooting begins
        _soundService.PlayShootingStart();

        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";
        _countdownDuration = _config.ShootingTimeSeconds;

        if (_state.Phase == MatchPhase.PreparationGroup1)
        {
            _state.Phase = MatchPhase.ShootingGroup1;
            _state.SetBothDisplays(_countdownDuration, _state.Display1.Group, AmpelColor.Green, endStr);
        }
        else
        {
            _state.Phase = MatchPhase.ShootingGroup2;
            _state.SetBothDisplays(_countdownDuration, _state.Display1.Group, AmpelColor.Green, endStr);
        }

        _state.ManualColorOverride = false;
        _stopwatch.Restart();
        _timer.Start();

        _logger.LogInformation("Shooting started. Phase: {Phase}, Group: {Group}", _state.Phase, _state.Display1.Group);
    }

    private void TransitionAfterShooting()
    {
        _timer.Stop();
        _stopwatch.Reset();

        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        if (_state.Phase == MatchPhase.ShootingGroup1 && _config.GroupSwitchEnabled && _config.Groups.Length > 1)
        {
            // Switch to group 2 preparation
            var group2Index = (_startingGroupIndex + 1) % _config.Groups.Length;
            var group2Name = _config.Groups[group2Index];

            // 2× horn — preparation for group 2
            _soundService.PlayPreparation();

            _state.Phase = MatchPhase.PreparationGroup2;
            _countdownDuration = _config.PreparationTimeSeconds;
            _state.SetBothDisplays(_countdownDuration, group2Name, AmpelColor.Red, endStr);

            _stopwatch.Restart();
            _timer.Start();

            _logger.LogInformation("Preparation for group 2: {Group}", group2Name);
        }
        else
        {
            // End completed
            // 3× horn — end complete
            _soundService.PlayEndCompleted();

            // Alternate starting group for next end (AB→CD→Score → CD→AB→Score → ...)
            if (_config.AlternateStartOrder && _config.Groups.Length > 1)
                _startingGroupIndex = (_startingGroupIndex + 1) % _config.Groups.Length;

            _state.Phase = MatchPhase.EndCompleted;
            _state.Status = TimerStatus.Stopped;
            _state.SetBothDisplays(0, _state.Display1.Group, AmpelColor.Red, endStr);

            _logger.LogInformation("End {End} completed. Next starting group: {Group}",
                endStr, _config.Groups[_startingGroupIndex]);
        }
    }

    private void TransitionToFinalShooting()
    {
        _timer.Stop();
        _stopwatch.Reset();

        // 1× horn — shooting begins for the active side
        _soundService.PlayShootingStart();

        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";
        _countdownDuration = _config.ShootingTimeSeconds;

        _state.Phase = MatchPhase.ShootingGroup1;
        _currentRound = 1;

        var (activeDisp, inactiveDisp) = GetActiveSideDisplays();
        activeDisp.TimeRemaining = _countdownDuration;
        activeDisp.Color = AmpelColor.Green;
        activeDisp.End = endStr;
        inactiveDisp.TimeRemaining = 0;
        inactiveDisp.Color = AmpelColor.Red;
        inactiveDisp.End = endStr;

        _state.ManualColorOverride = false;
        _stopwatch.Restart();
        _timer.Start();

        _logger.LogInformation("Final shooting started. Side: {Side}, Round: 1", _state.CurrentSide);
    }

    private void TransitionFinalAfterShooting()
    {
        _timer.Stop();
        _stopwatch.Reset();

        if (_activePreset?.Final == null) return;

        var final = _activePreset.Final;
        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        // Add arrows for current side
        if (_state.CurrentSide == "left")
            _state.ArrowCountLeft += final.ArrowsPerSide;
        else
            _state.ArrowCountRight += final.ArrowsPerSide;

        // Check if both sides have shot all arrows
        var totalRoundsPerSide = final.TotalArrowsPerEnd / final.ArrowsPerSide;
        var leftDone = _state.ArrowCountLeft >= final.TotalArrowsPerEnd;
        var rightDone = _state.ArrowCountRight >= final.TotalArrowsPerEnd;

        if (leftDone && rightDone)
        {
            // 3× horn — end completed
            _soundService.PlayEndCompleted();

            _state.Phase = MatchPhase.EndCompleted;
            _state.Status = TimerStatus.Stopped;
            SetBothFinalDisplays(0, AmpelColor.Red, endStr);

            _logger.LogInformation("Final end {End} completed. Left: {L}, Right: {R}",
                endStr, _state.ArrowCountLeft, _state.ArrowCountRight);
        }
        else
        {
            // Switch sides — 1× horn signals next shooter
            _soundService.PlayShootingStart();

            _state.CurrentSide = _state.CurrentSide == "left" ? "right" : "left";
            _currentRound++;

            _countdownDuration = _config.ShootingTimeSeconds;

            var (activeDisp, inactiveDisp) = GetActiveSideDisplays();
            activeDisp.TimeRemaining = _countdownDuration;
            activeDisp.Color = AmpelColor.Green;
            activeDisp.End = endStr;
            inactiveDisp.TimeRemaining = 0;
            inactiveDisp.Color = AmpelColor.Red;
            inactiveDisp.End = endStr;

            _state.ManualColorOverride = false;
            _stopwatch.Restart();
            _timer.Start();

            _logger.LogInformation("Final side switch to {Side}, round {Round}", _state.CurrentSide, _currentRound);
        }
    }

    #region Helper methods for final mode display mapping

    private (DisplayState active, DisplayState inactive) GetActiveSideDisplays()
    {
        // Display1 is always the left side, Display2 is always the right side.
        // Display1Side/Display2Side only affect RS485 serial output mapping.
        return _state.CurrentSide == "left"
            ? (_state.Display1, _state.Display2)
            : (_state.Display2, _state.Display1);
    }

    private DisplayState GetActiveDisplay()
    {
        var (active, _) = GetActiveSideDisplays();
        return active;
    }

    private void SetBothFinalDisplays(int time, AmpelColor color, string end)
    {
        var sides = _activePreset?.Final?.Sides ?? ["1", "2"];
        // Display1 is always left, Display2 is always right.
        _state.Display1.TimeRemaining = time;
        _state.Display1.Group = sides[0];
        _state.Display1.Color = color;
        _state.Display1.End = end;
        _state.Display2.TimeRemaining = time;
        _state.Display2.Group = sides.Length > 1 ? sides[1] : sides[0];
        _state.Display2.Color = color;
        _state.Display2.End = end;
    }

    #endregion

    #region Public API

    public void Start()
    {
        lock (_lock)
        {
            if (_state.Status == TimerStatus.Running) return;

            _state.ManualColorOverride = false;

            if (_state.Mode == OperatingMode.Final)
            {
                StartFinal();
            }
            else
            {
                StartStandard();
            }
        }

        RaiseStateChanged();
    }

    private void StartStandard()
    {
        var group1Name = _config.Groups[_startingGroupIndex % _config.Groups.Length];
        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        // 2× horn — preparation
        _soundService.PlayPreparation();

        _state.Phase = MatchPhase.PreparationGroup1;
        _state.Status = TimerStatus.Running;
        _countdownDuration = _config.PreparationTimeSeconds;
        _state.SetBothDisplays(_countdownDuration, group1Name, AmpelColor.Red, endStr);

        _stopwatch.Restart();
        _timer.Start();

        _logger.LogInformation("Standard end started. Group 1: {Group}, Prep: {Prep}s, Shooting: {Shoot}s",
            group1Name, _config.PreparationTimeSeconds, _config.ShootingTimeSeconds);
    }

    private void StartFinal()
    {
        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";

        // Reset arrow counters for new end
        _state.ArrowCountLeft = 0;
        _state.ArrowCountRight = 0;
        _currentRound = 0;

        // 2× horn — preparation (both sides)
        _soundService.PlayPreparation();

        _state.Phase = MatchPhase.PreparationGroup1;
        _state.Status = TimerStatus.Running;
        _countdownDuration = _config.PreparationTimeSeconds;
        SetBothFinalDisplays(_countdownDuration, AmpelColor.Red, endStr);

        _stopwatch.Restart();
        _timer.Start();

        _logger.LogInformation("Final end started. Starting side: {Side}, Prep: {Prep}s",
            _state.CurrentSide, _config.PreparationTimeSeconds);
    }

    public void Stop()
    {
        lock (_lock)
        {
            _timer.Stop();
            _stopwatch.Stop();
            _state.Status = TimerStatus.Stopped;
            _state.Phase = MatchPhase.Idle;
            _state.ManualColorOverride = false;

            var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";
            if (_state.Mode == OperatingMode.Final)
                SetBothFinalDisplays(0, AmpelColor.Red, endStr);
            else
                _state.SetBothDisplays(0, _state.Display1.Group, AmpelColor.Red, endStr);

            _logger.LogInformation("Timer stopped");
        }

        RaiseStateChanged();
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Running) return;

            _timer.Stop();
            _stopwatch.Stop();
            _state.Status = TimerStatus.Paused;
            _logger.LogInformation("Timer paused at {Seconds}s", _state.Display1.TimeRemaining);
        }

        RaiseStateChanged();
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Paused) return;

            // Recalculate countdown based on remaining display time
            _countdownDuration = _state.Display1.TimeRemaining;
            _state.Status = TimerStatus.Running;
            _stopwatch.Restart();
            _timer.Start();

            _logger.LogInformation("Timer resumed at {Seconds}s, Phase: {Phase}", _countdownDuration, _state.Phase);
        }

        RaiseStateChanged();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _timer.Stop();
            _stopwatch.Reset();
            _state.Status = TimerStatus.Stopped;
            _state.Phase = MatchPhase.Idle;
            _state.ManualColorOverride = false;
            _config.CurrentEnd = 1;
            _state.CurrentEnd = 1;
            _state.ArrowCountLeft = 0;
            _state.ArrowCountRight = 0;
            _currentRound = 0;
            _startingGroupIndex = 0;

            var endStr = $"1/{_config.TotalEnds}";
            if (_state.Mode == OperatingMode.Final)
                SetBothFinalDisplays(0, AmpelColor.Red, endStr);
            else
                _state.SetBothDisplays(0, _config.Groups[0], AmpelColor.Red, endStr);

            _logger.LogInformation("Reset. Ends: {Ends}", _config.TotalEnds);
        }

        RaiseStateChanged();
    }

    public void Skip()
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Running) return;
            if (!_config.SkipEnabled) return;

            // Skip only works during shooting phases, NOT preparation
            switch (_state.Phase)
            {
                case MatchPhase.ShootingGroup1:
                case MatchPhase.ShootingGroup2:
                    _logger.LogInformation("Skip triggered during {Phase}", _state.Phase);
                    if (_state.Mode == OperatingMode.Final)
                        TransitionFinalAfterShooting();
                    else
                        TransitionAfterShooting();
                    break;
                default:
                    _logger.LogDebug("Skip ignored — not in shooting phase (current: {Phase})", _state.Phase);
                    break;
            }
        }

        RaiseStateChanged();
    }

    public void EmergencyStop()
    {
        lock (_lock)
        {
            _timer.Stop();
            _stopwatch.Stop();

            _emergencyFrozenTime = _state.Display1.TimeRemaining;
            _state.Status = TimerStatus.Paused;
            _state.Phase = MatchPhase.EmergencyStopped;

            var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";
            if (_state.Mode == OperatingMode.Final)
                SetBothFinalDisplays(_emergencyFrozenTime, AmpelColor.Red, endStr);
            else
                _state.SetBothDisplays(_emergencyFrozenTime, _state.Display1.Group, AmpelColor.Red, endStr);

            _logger.LogWarning("EMERGENCY STOP at {Seconds}s", _emergencyFrozenTime);
        }

        // 5× horn — must be outside lock to avoid blocking
        _soundService.PlayEmergencyStop();

        RaiseStateChanged();
    }

    public void SetGroup(string group)
    {
        lock (_lock)
        {
            _state.Display1.Group = group;
            _state.Display2.Group = group;
            _logger.LogInformation("Group set to {Group}", group);
        }
        RaiseStateChanged();
    }

    public void SetDuration(int seconds)
    {
        lock (_lock)
        {
            _config.ShootingTimeSeconds = seconds;
            _logger.LogInformation("Shooting duration set to {Duration}s", seconds);
        }
        RaiseStateChanged();
    }

    public void SetPreparationTime(int seconds)
    {
        lock (_lock)
        {
            _config.PreparationTimeSeconds = seconds;
            _logger.LogInformation("Preparation time set to {Duration}s", seconds);
        }
        RaiseStateChanged();
    }

    public void SetColor(AmpelColor color)
    {
        lock (_lock)
        {
            _state.ManualColorOverride = true;
            _state.Display1.Color = color;
            _state.Display2.Color = color;
            _logger.LogInformation("Manual color override: {Color}", color);
        }
        RaiseStateChanged();
    }

    public void NextEnd()
    {
        lock (_lock)
        {
            if (_config.CurrentEnd < _config.TotalEnds)
            {
                _config.CurrentEnd++;
                _state.CurrentEnd = _config.CurrentEnd;
                UpdateEndDisplay();
                _logger.LogInformation("Next end: {End}", _state.Display1.End);
            }
        }
        RaiseStateChanged();
    }

    public void PreviousEnd()
    {
        lock (_lock)
        {
            if (_config.CurrentEnd > 1)
            {
                _config.CurrentEnd--;
                _state.CurrentEnd = _config.CurrentEnd;
                UpdateEndDisplay();
                _logger.LogInformation("Previous end: {End}", _state.Display1.End);
            }
        }
        RaiseStateChanged();
    }

    public void SetTotalEnds(int totalEnds)
    {
        lock (_lock)
        {
            _config.TotalEnds = totalEnds;
            _state.TotalEnds = totalEnds;
            if (_config.CurrentEnd > totalEnds)
            {
                _config.CurrentEnd = totalEnds;
                _state.CurrentEnd = totalEnds;
            }
            UpdateEndDisplay();
            _logger.LogInformation("Total ends set to {TotalEnds}", totalEnds);
        }
        RaiseStateChanged();
    }

    public void SetStartingGroup(int groupIndex)
    {
        lock (_lock)
        {
            _startingGroupIndex = groupIndex % _config.Groups.Length;
            _logger.LogInformation("Starting group index set to {Index} ({Group})",
                _startingGroupIndex, _config.Groups[_startingGroupIndex]);
        }
    }

    public void SetStartingSide(string side)
    {
        lock (_lock)
        {
            _state.CurrentSide = side.ToLower();
            _state.StartingSide = side.ToLower();
            _logger.LogInformation("Starting side set to {Side}", side);
        }
        RaiseStateChanged();
    }

    public void SwitchSide()
    {
        lock (_lock)
        {
            if (_state.Mode != OperatingMode.Final) return;
            if (_state.Status != TimerStatus.Running) return;

            // Same as Skip in final mode
            if (_state.Phase is MatchPhase.ShootingGroup1 or MatchPhase.ShootingGroup2)
            {
                _logger.LogInformation("Side switch triggered");
                TransitionFinalAfterShooting();
            }
        }
        RaiseStateChanged();
    }

    public void ApplyPreset(Preset preset)
    {
        lock (_lock)
        {
            _activePreset = preset;
            _config.ShootingTimeSeconds = preset.Timer.ShootingTime;
            _config.PreparationTimeSeconds = preset.Timer.PreparationTime;
            _config.WarningTimeSeconds = preset.Timer.WarningTime;
            _config.Groups = (string[])preset.Groups.Names.Clone();
            _config.AlternateStartOrder = preset.Groups.AlternateStartOrder;
            _config.GroupSwitchEnabled = preset.Options.GroupSwitchEnabled;
            _config.SkipEnabled = preset.Options.SkipEnabled;
            _config.TotalEnds = preset.Match.TotalEnds;
            _config.CurrentEnd = 1;
            _state.TotalEnds = preset.Match.TotalEnds;
            _state.CurrentEnd = 1;
            _startingGroupIndex = 0;
            _state.ArrowCountLeft = 0;
            _state.ArrowCountRight = 0;
            _currentRound = 0;

            if (preset.IsFinalMode)
            {
                _state.Mode = OperatingMode.Final;
                _config.SkipEnabled = preset.Final?.SkipEnabled ?? true;
                var sides = preset.Final?.Sides ?? ["1", "2"];
                _state.CurrentSide = "left";
                _state.StartingSide = "left";
            }
            else
            {
                _state.Mode = OperatingMode.Standard;
            }

            // Reset displays
            var endStr = $"1/{_config.TotalEnds}";
            if (_state.Mode == OperatingMode.Final)
                SetBothFinalDisplays(0, AmpelColor.Red, endStr);
            else
                _state.SetBothDisplays(0, _config.Groups[0], AmpelColor.Red, endStr);

            _state.Status = TimerStatus.Stopped;
            _state.Phase = MatchPhase.Idle;

            _logger.LogInformation("Preset applied: {Name} (Mode: {Mode})", preset.Name, _state.Mode);
        }
        RaiseStateChanged();
    }

    public void SetMode(OperatingMode mode)
    {
        lock (_lock)
        {
            _state.Mode = mode;
            _logger.LogInformation("Mode set to {Mode}", mode);
        }
        RaiseStateChanged();
    }

    #endregion

    private void UpdateEndDisplay()
    {
        var endStr = $"{_config.CurrentEnd}/{_config.TotalEnds}";
        _state.Display1.End = endStr;
        _state.Display2.End = endStr;
    }

    private void RaiseStateChanged()
    {
        AmpelState snapshot;
        lock (_lock) snapshot = _state.Clone();
        StateChanged?.Invoke(this, snapshot);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _stopwatch.Stop();
    }
}
