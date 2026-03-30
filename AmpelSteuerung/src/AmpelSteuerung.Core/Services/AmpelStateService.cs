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

    public AmpelState CurrentState
    {
        get
        {
            lock (_lock) return _state.Clone();
        }
    }

    public TimerConfig Config
    {
        get
        {
            lock (_lock)
            {
                return new TimerConfig
                {
                    DurationSeconds = _config.DurationSeconds,
                    WarningTimeSeconds = _config.WarningTimeSeconds,
                    TotalEnds = _config.TotalEnds,
                    CurrentEnd = _config.CurrentEnd,
                    Groups = (string[])_config.Groups.Clone()
                };
            }
        }
    }

    public event EventHandler<AmpelState>? StateChanged;

    public AmpelStateService(ISoundService soundService, ILogger<AmpelStateService> logger, AmpelConfiguration configuration)
    {
        _soundService = soundService;
        _logger = logger;

        _config.DurationSeconds = configuration.DefaultDurationSeconds;
        _config.WarningTimeSeconds = configuration.WarningTimeSeconds;

        _state.TimeRemaining = _config.DurationSeconds;
        _state.Group = configuration.LastGroup;
        _config.TotalEnds = configuration.LastTotalEnds;
        _state.End = $"1/{_config.TotalEnds}";

        // Use a fast timer (50ms) for precision — we check elapsed time via Stopwatch
        _timer = new System.Timers.Timer(50);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Running) return;

            var elapsedSeconds = (int)_stopwatch.Elapsed.TotalSeconds;
            var newTimeRemaining = Math.Max(0, _config.DurationSeconds - elapsedSeconds);

            if (newTimeRemaining == _state.TimeRemaining) return;

            _state.TimeRemaining = newTimeRemaining;

            // Auto color transitions (only if no manual override)
            if (!_state.ManualColorOverride)
            {
                if (_state.TimeRemaining == 0)
                {
                    _state.Color = AmpelColor.Red;
                    _state.Status = TimerStatus.Stopped;
                    _timer.Stop();
                    _stopwatch.Stop();
                    _soundService.PlayTimerEnd();
                    _logger.LogInformation("Timer ended. Color → Red");
                }
                else if (_state.TimeRemaining <= _config.WarningTimeSeconds && _state.Color == AmpelColor.Green)
                {
                    _state.Color = AmpelColor.Yellow;
                    _soundService.PlayWarning();
                    _logger.LogInformation("Warning phase. Color → Yellow. {Seconds}s remaining", _state.TimeRemaining);
                }
            }
        }

        RaiseStateChanged();
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_state.Status == TimerStatus.Running) return;

            _state.ManualColorOverride = false;
            _state.Color = AmpelColor.Green;
            _state.Status = TimerStatus.Running;
            _state.TimeRemaining = _config.DurationSeconds;
            _stopwatch.Restart();
            _timer.Start();

            _soundService.PlayTimerStart();
            _logger.LogInformation("Timer started. Duration: {Duration}s, Group: {Group}", _config.DurationSeconds, _state.Group);
        }

        RaiseStateChanged();
    }

    public void Stop()
    {
        lock (_lock)
        {
            _timer.Stop();
            _stopwatch.Stop();
            _state.Status = TimerStatus.Stopped;
            _state.Color = AmpelColor.Red;
            _state.ManualColorOverride = false;
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
            _logger.LogInformation("Timer paused at {Seconds}s", _state.TimeRemaining);
        }

        RaiseStateChanged();
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_state.Status != TimerStatus.Paused) return;

            // Adjust stopwatch to account for already elapsed time
            var alreadyElapsed = _config.DurationSeconds - _state.TimeRemaining;
            _stopwatch.Restart();
            // We need to "fast-forward" the stopwatch concept — instead, store offset
            _config.DurationSeconds = _state.TimeRemaining; // Treat remaining as new duration
            _state.Status = TimerStatus.Running;
            _timer.Start();
            _logger.LogInformation("Timer resumed at {Seconds}s", _state.TimeRemaining);
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
            _state.TimeRemaining = _config.DurationSeconds;
            _state.Color = AmpelColor.Red;
            _state.ManualColorOverride = false;
            _logger.LogInformation("Timer reset to {Duration}s", _config.DurationSeconds);
        }

        RaiseStateChanged();
    }

    public void SetGroup(string group)
    {
        lock (_lock)
        {
            _state.Group = group;
            _logger.LogInformation("Group set to {Group}", group);
        }

        RaiseStateChanged();
    }

    public void SetDuration(int seconds)
    {
        lock (_lock)
        {
            _config.DurationSeconds = seconds;
            if (_state.Status == TimerStatus.Stopped)
            {
                _state.TimeRemaining = seconds;
            }
            _logger.LogInformation("Duration set to {Duration}s", seconds);
        }

        RaiseStateChanged();
    }

    public void SetColor(AmpelColor color)
    {
        lock (_lock)
        {
            _state.Color = color;
            _state.ManualColorOverride = true;
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
                _state.End = $"{_config.CurrentEnd}/{_config.TotalEnds}";
                _logger.LogInformation("Next end: {End}", _state.End);
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
                _state.End = $"{_config.CurrentEnd}/{_config.TotalEnds}";
                _logger.LogInformation("Previous end: {End}", _state.End);
            }
        }

        RaiseStateChanged();
    }

    public void SetTotalEnds(int totalEnds)
    {
        lock (_lock)
        {
            _config.TotalEnds = totalEnds;
            if (_config.CurrentEnd > totalEnds) _config.CurrentEnd = totalEnds;
            _state.End = $"{_config.CurrentEnd}/{_config.TotalEnds}";
            _logger.LogInformation("Total ends set to {TotalEnds}", totalEnds);
        }

        RaiseStateChanged();
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
