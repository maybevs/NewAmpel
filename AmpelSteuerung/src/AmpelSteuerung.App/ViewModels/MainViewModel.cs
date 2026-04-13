using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Models;
using AmpelSteuerung.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.App.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IAmpelStateService _stateService;
    private readonly ISerialService _serialService;
    private readonly ISoundService _soundService;
    private readonly PresetEngine _presetEngine;
    private readonly AmpelConfiguration _config;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Dispatcher _dispatcher;

    // Common display
    [ObservableProperty] private string _timeDisplay = "00:00";
    [ObservableProperty] private string _currentGroup = "AB";
    [ObservableProperty] private string _currentEnd = "1/10";
    [ObservableProperty] private AmpelColor _currentColor = AmpelColor.Red;
    [ObservableProperty] private TimerStatus _timerStatus = TimerStatus.Stopped;
    [ObservableProperty] private string _colorBrush = "#CC0000";
    [ObservableProperty] private string _phaseText = "Bereit";

    // Display 1 (standard mode: same as main, final mode: left/right)
    [ObservableProperty] private string _display1Time = "00:00";
    [ObservableProperty] private string _display1Group = "";
    [ObservableProperty] private string _display1ColorBrush = "#CC0000";

    // Display 2
    [ObservableProperty] private string _display2Time = "00:00";
    [ObservableProperty] private string _display2Group = "";
    [ObservableProperty] private string _display2ColorBrush = "#CC0000";

    // Mode
    [ObservableProperty] private bool _isFinalMode;
    [ObservableProperty] private bool _finalsSingleDisplay;
    [ObservableProperty] private string _currentSide = "left";
    [ObservableProperty] private bool _isStartSideLeft = true;
    [ObservableProperty] private bool _isStartSideRight;
    [ObservableProperty] private string _arrowCountLeftText = "0";
    [ObservableProperty] private string _arrowCountRightText = "0";
    [ObservableProperty] private string _arrowDotsLeft = "";
    [ObservableProperty] private string _arrowDotsRight = "";

    // Serial
    [ObservableProperty] private string _selectedComPort = "";
    [ObservableProperty] private bool _isSerialConnected;
    [ObservableProperty] private string _connectionStatusText = "Getrennt";
    [ObservableProperty] private string? _serialError;
    [ObservableProperty] private ObservableCollection<string> _availableComPorts = new();

    // Configuration
    [ObservableProperty] private int _timerDuration = 120;
    [ObservableProperty] private int _preparationTime = 10;
    [ObservableProperty] private int _totalEnds = 10;
    [ObservableProperty] private bool _soundEnabled = true;
    [ObservableProperty] private string _display1SideLabel = "Links";
    [ObservableProperty] private string _display2SideLabel = "Rechts";

    // Presets
    [ObservableProperty] private ObservableCollection<Preset> _presets = new();
    [ObservableProperty] private Preset? _selectedPreset;
    [ObservableProperty] private string _presetStatus = "";

    // UI state
    [ObservableProperty] private bool _isStartEnabled = true;
    [ObservableProperty] private bool _isPauseEnabled;
    [ObservableProperty] private bool _isResumeEnabled;
    [ObservableProperty] private bool _isSkipEnabled;
    [ObservableProperty] private bool _isEmergencyStopped;
    [ObservableProperty] private bool _isBeamerOpen;
    [ObservableProperty] private bool _isStopped = true;

    // Idle display
    [ObservableProperty] private IdleDisplayMode _idleMode = IdleDisplayMode.Clock;
    [ObservableProperty] private string _idleMessage = "";
    [ObservableProperty] private bool _idleMessageScroll;
    [ObservableProperty] private string _idleScrollText = "";
    [ObservableProperty] private bool _isIdleModeOff;
    [ObservableProperty] private bool _isIdleModeClock = true;
    [ObservableProperty] private bool _isIdleModeMessage;
    [ObservableProperty] private bool _isIdleModeBoth;
    [ObservableProperty] private ObservableCollection<string> _quickMessages = new();
    [ObservableProperty] private bool _showIdleOnBeamer;
    [ObservableProperty] private string _currentClock = "";
    [ObservableProperty] private bool _showIdleClock;
    [ObservableProperty] private bool _showIdleMessage;

    // Timer format
    [ObservableProperty] private bool _isTimeFormatMinutes;
    [ObservableProperty] private bool _isTimeFormatSeconds = true;

    // Clock timer for beamer idle display
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _displayRefreshTimer;
    private readonly string _clockFormat;

    // Beamer screen selection
    [ObservableProperty] private ObservableCollection<string> _availableScreens = new();
    [ObservableProperty] private int _selectedScreenIndex;

    public MainViewModel(
        IAmpelStateService stateService,
        ISerialService serialService,
        ISoundService soundService,
        PresetEngine presetEngine,
        AmpelConfiguration config,
        ILogger<MainViewModel> logger)
    {
        _stateService = stateService;
        _serialService = serialService;
        _soundService = soundService;
        _presetEngine = presetEngine;
        _config = config;
        _logger = logger;
        _dispatcher = Application.Current.Dispatcher;

        _timerDuration = config.DefaultShootingTime;
        _preparationTime = config.DefaultPreparationTime;
        _totalEnds = config.LastTotalEnds;
        _soundEnabled = config.SoundEnabled;
        _soundService.IsEnabled = config.SoundEnabled;
        _soundService.Volume = config.SoundVolume;
        _finalsSingleDisplay = config.FinalsSingleDisplay;

        var swapped = config.Display1Side == "right";
        _display1SideLabel = swapped ? "Display 2" : "Display 1";
        _display2SideLabel = swapped ? "Display 1" : "Display 2";

        _stateService.StateChanged += OnStateChanged;
        _serialService.ConnectionChanged += OnConnectionChanged;
        _presetEngine.StatusChanged += OnPresetStatusChanged;

        _presetEngine.LoadPresets(config.PresetsDirectory);
        foreach (var p in _presetEngine.AvailablePresets)
            Presets.Add(p);

        // Load quick messages from config
        foreach (var msg in config.Idle.QuickMessages)
            QuickMessages.Add(msg);

        // Clock timer for beamer idle display
        _clockFormat = config.Idle.ClockFormat;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => CurrentClock = DateTime.Now.ToString(_clockFormat);
        _clockTimer.Start();
        CurrentClock = DateTime.Now.ToString(_clockFormat);

        // High-frequency UI refresh for smooth timer display
        _displayRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _displayRefreshTimer.Tick += OnDisplayRefreshTick;
        _displayRefreshTimer.Start();

        UpdateFromState(_stateService.CurrentState);
        RefreshComPorts();
        RefreshScreens();
    }

    private void OnDisplayRefreshTick(object? sender, EventArgs e)
    {
        if (TimerStatus != Core.Models.TimerStatus.Running) return;
        var state = _stateService.CurrentState;
        if (state.TimeFormat == TimeDisplayFormat.Finals)
        {
            TimeDisplay = FormatTimeMs(state.Display1.TimeRemainingMs);
            Display1Time = FormatTimeMs(state.Display1.TimeRemainingMs);
            Display2Time = FormatTimeMs(state.Display2.TimeRemainingMs);
        }
        else
        {
            TimeDisplay = FormatTime(state.Display1.TimeRemaining);
            Display1Time = FormatTime(state.Display1.TimeRemaining);
            Display2Time = FormatTime(state.Display2.TimeRemaining);
        }
    }

    private void OnStateChanged(object? sender, AmpelState state)
    {
        _dispatcher.BeginInvoke(() => UpdateFromState(state));
    }

    private void OnConnectionChanged(object? sender, bool connected)
    {
        _dispatcher.BeginInvoke(() =>
        {
            IsSerialConnected = connected;
            ConnectionStatusText = connected ? $"Verbunden ({_serialService.CurrentPort})" : "Getrennt";
            SerialError = _serialService.ErrorMessage;
        });
    }

    private void OnPresetStatusChanged(object? sender, string status)
    {
        _dispatcher.BeginInvoke(() => PresetStatus = status);
    }

    private void UpdateFromState(AmpelState state)
    {
        IsFinalMode = state.Mode == OperatingMode.Final;
        TimerStatus = state.Status;
        CurrentEnd = state.End;

        // Display 1
        Display1Time = state.TimeFormat == TimeDisplayFormat.Finals
            ? FormatTimeMs(state.Display1.TimeRemainingMs)
            : FormatTime(state.Display1.TimeRemaining);
        Display1Group = state.Display1.Group;
        Display1ColorBrush = ColorToHex(state.Display1.Color);

        // Display 2
        Display2Time = state.TimeFormat == TimeDisplayFormat.Finals
            ? FormatTimeMs(state.Display2.TimeRemainingMs)
            : FormatTime(state.Display2.TimeRemaining);
        Display2Group = state.Display2.Group;
        Display2ColorBrush = ColorToHex(state.Display2.Color);

        // Main display (standard = display1, final = active side)
        TimeDisplay = state.TimeFormat == TimeDisplayFormat.Finals
            ? FormatTimeMs(state.Display1.TimeRemainingMs)
            : FormatTime(state.Display1.TimeRemaining);
        CurrentGroup = state.Display1.Group;
        CurrentColor = state.Display1.Color;
        ColorBrush = Display1ColorBrush;

        // Phase text
        PhaseText = state.Phase switch
        {
            MatchPhase.Idle => "Bereit",
            MatchPhase.PreparationGroup1 => "Vorbereitung",
            MatchPhase.ShootingGroup1 => "Schießzeit",
            MatchPhase.PreparationGroup2 => "Vorbereitung Gr. 2",
            MatchPhase.ShootingGroup2 => "Schießzeit Gr. 2",
            MatchPhase.EndCompleted => "Passe beendet",
            MatchPhase.EmergencyStopped => "⚠ NOTFALL-STOPP",
            _ => ""
        };

        // Final mode specifics
        CurrentSide = state.CurrentSide;
        IsStartSideLeft = state.StartingSide == "left";
        IsStartSideRight = state.StartingSide == "right";
        ArrowCountLeftText = state.ArrowCountLeft.ToString();
        ArrowCountRightText = state.ArrowCountRight.ToString();

        // Button states
        IsStartEnabled = state.Status == Core.Models.TimerStatus.Stopped
                         || state.Phase == MatchPhase.EndCompleted
                         || state.Phase == MatchPhase.Idle;
        IsPauseEnabled = state.Status == Core.Models.TimerStatus.Running;
        IsResumeEnabled = state.Status == Core.Models.TimerStatus.Paused
                          && state.Phase != MatchPhase.EmergencyStopped;
        IsSkipEnabled = state.Status == Core.Models.TimerStatus.Running
                        && (state.Phase is MatchPhase.ShootingGroup1 or MatchPhase.ShootingGroup2);
        IsEmergencyStopped = state.Phase == MatchPhase.EmergencyStopped;
        IsStopped = state.Status == Core.Models.TimerStatus.Stopped;

        // Idle mode
        IdleMode = state.IdleMode;
        IdleMessageScroll = state.IdleMessageScroll;
        IdleScrollText = state.IdleMessageScroll ? "← Scrollend" : (state.IdleMessage.Length > 0 ? "Statisch" : "");
        IsIdleModeOff = state.IdleMode == IdleDisplayMode.Off;
        IsIdleModeClock = state.IdleMode == IdleDisplayMode.Clock;
        IsIdleModeMessage = state.IdleMode == IdleDisplayMode.Message;
        IsIdleModeBoth = state.IdleMode == IdleDisplayMode.Both;

        // Beamer idle overlay
        ShowIdleOnBeamer = IsStopped && state.IdleMode != IdleDisplayMode.Off;
        ShowIdleClock = state.IdleMode is IdleDisplayMode.Clock or IdleDisplayMode.Both;
        ShowIdleMessage = state.IdleMode is IdleDisplayMode.Message or IdleDisplayMode.Both;

        // Timer format
        IsTimeFormatMinutes = state.TimeFormat == TimeDisplayFormat.Minutes;
        IsTimeFormatSeconds = state.TimeFormat == TimeDisplayFormat.Seconds;
    }

    private string FormatTime(int seconds)
    {
        if (IsTimeFormatSeconds)
            return seconds.ToString();
        return $"{seconds / 60:D2}:{seconds % 60:D2}";
    }

    private static string FormatTimeMs(int totalMs)
    {
        var totalCs = totalMs / 10;
        var secs = totalCs / 100;
        var cs = totalCs % 100;
        return $"{secs:D2}:{cs:D2}";
    }

    private static string ColorToHex(AmpelColor c) => c switch
    {
        AmpelColor.Red => "#CC0000",
        AmpelColor.Green => "#00AA00",
        AmpelColor.Yellow => "#DDAA00",
        _ => "#CC0000"
    };

    // === Commands ===

    [RelayCommand]
    private void StartTimer()
    {
        if (_stateService.CurrentState.Phase == MatchPhase.EndCompleted)
        {
            _stateService.NextEnd();
        }
        _stateService.Start();
    }

    [RelayCommand]
    private void PauseTimer() => _stateService.Pause();

    [RelayCommand]
    private void ResumeTimer() => _stateService.Resume();

    [RelayCommand]
    private void StopTimer() => _stateService.Stop();

    [RelayCommand]
    private void ResetTimer() => _stateService.Reset();

    [RelayCommand]
    private void SkipTimer() => _stateService.Skip();

    [RelayCommand]
    private void EmergencyStop() => _stateService.EmergencyStop();

    [RelayCommand]
    private void ToggleStartPause()
    {
        var state = _stateService.CurrentState;
        switch (state.Status)
        {
            case Core.Models.TimerStatus.Stopped:
                StartTimer();
                break;
            case Core.Models.TimerStatus.Running:
                _stateService.Pause();
                break;
            case Core.Models.TimerStatus.Paused:
                _stateService.Resume();
                break;
        }
    }

    [RelayCommand]
    private void SetGroupAB() => _stateService.SetGroup("AB");

    [RelayCommand]
    private void SetGroupCD() => _stateService.SetGroup("CD");

    [RelayCommand]
    private void SetStartGroupAB() => _stateService.SetStartingGroup(0);

    [RelayCommand]
    private void SetStartGroupCD() => _stateService.SetStartingGroup(1);

    [RelayCommand]
    private void NextEnd() => _stateService.NextEnd();

    [RelayCommand]
    private void PreviousEnd() => _stateService.PreviousEnd();

    [RelayCommand]
    private void SetColorRed() => _stateService.SetColor(AmpelColor.Red);

    [RelayCommand]
    private void SetColorGreen() => _stateService.SetColor(AmpelColor.Green);

    [RelayCommand]
    private void SetColorYellow() => _stateService.SetColor(AmpelColor.Yellow);

    // Final mode
    [RelayCommand]
    private void SetStartSideLeft() => _stateService.SetStartingSide("left");

    [RelayCommand]
    private void SetStartSideRight() => _stateService.SetStartingSide("right");

    [RelayCommand]
    private void SwitchSide() => _stateService.SwitchSide();

    // Serial
    [RelayCommand]
    private void RefreshComPorts()
    {
        AvailableComPorts.Clear();
        foreach (var port in _serialService.GetAvailablePorts())
            AvailableComPorts.Add(port);
        if (AvailableComPorts.Count > 0 && string.IsNullOrEmpty(SelectedComPort))
            SelectedComPort = AvailableComPorts[0];
    }

    [RelayCommand]
    private void ConnectSerial()
    {
        if (string.IsNullOrEmpty(SelectedComPort)) return;
        _serialService.Connect(SelectedComPort, _config.BaudRate);
        _serialService.StartBroadcast();
    }

    [RelayCommand]
    private void DisconnectSerial()
    {
        _serialService.StopBroadcast();
        _serialService.Disconnect();
    }

    // Display mapping
    [RelayCommand]
    private void SwapDisplaySides()
    {
        var tmp = _stateService.Display1Side;
        _stateService.Display1Side = _stateService.Display2Side;
        _stateService.Display2Side = tmp;
        var swapped = _stateService.Display1Side == "right";
        Display1SideLabel = swapped ? "Display 2" : "Display 1";
        Display2SideLabel = swapped ? "Display 1" : "Display 2";
        _config.Display1Side = _stateService.Display1Side;
        _config.Display2Side = _stateService.Display2Side;
    }

    // Idle display
    [RelayCommand]
    private void SetIdleModeOff() => _stateService.SetIdleMode(IdleDisplayMode.Off);

    [RelayCommand]
    private void SetIdleModeClock() => _stateService.SetIdleMode(IdleDisplayMode.Clock);

    [RelayCommand]
    private void SetIdleModeMessage() => _stateService.SetIdleMode(IdleDisplayMode.Message);

    [RelayCommand]
    private void SetIdleModeBoth() => _stateService.SetIdleMode(IdleDisplayMode.Both);

    [RelayCommand]
    private void CycleIdleMode()
    {
        var next = _stateService.CurrentState.IdleMode switch
        {
            IdleDisplayMode.Clock => IdleDisplayMode.Message,
            IdleDisplayMode.Message => IdleDisplayMode.Both,
            IdleDisplayMode.Both => IdleDisplayMode.Off,
            IdleDisplayMode.Off => IdleDisplayMode.Clock,
            _ => IdleDisplayMode.Clock
        };
        _stateService.SetIdleMode(next);
    }

    [RelayCommand]
    private void SetTimeFormatToMinutes() => _stateService.SetTimeFormat(TimeDisplayFormat.Minutes);

    [RelayCommand]
    private void SetTimeFormatToSeconds() => _stateService.SetTimeFormat(TimeDisplayFormat.Seconds);

    [RelayCommand]
    private void ClearIdleMessage()
    {
        IdleMessage = "";
        _stateService.ClearIdleMessage();
    }

    [RelayCommand]
    private void SetQuickMessage(string message)
    {
        IdleMessage = message;
        _stateService.SetIdleMessage(message);
    }

    // Presets
    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedPreset == null) return;
        _presetEngine.ApplyPreset(SelectedPreset);
        IsFinalMode = SelectedPreset.IsFinalMode;
        TimerDuration = SelectedPreset.Timer.ShootingTime;
        PreparationTime = SelectedPreset.Timer.PreparationTime;
        TotalEnds = SelectedPreset.Match.TotalEnds;
    }

    // Beamer
    [RelayCommand]
    private void ToggleBeamer()
    {
        IsBeamerOpen = !IsBeamerOpen;
    }

    [RelayCommand]
    private void RefreshScreens()
    {
        AvailableScreens.Clear();
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            var label = s.Primary ? "Hauptbildschirm" : $"Bildschirm {i + 1}";
            AvailableScreens.Add($"{label} ({s.Bounds.Width}x{s.Bounds.Height})");
        }

        // Default to configured or first non-primary
        var preferred = _config.BeamerMonitor;
        if (preferred >= 0 && preferred < screens.Length)
            SelectedScreenIndex = preferred;
        else
        {
            var idx = Array.FindIndex(screens, s => !s.Primary);
            SelectedScreenIndex = idx >= 0 ? idx : 0;
        }
    }

    // Property change handlers
    partial void OnTimerDurationChanged(int value) => _stateService.SetDuration(value);
    partial void OnPreparationTimeChanged(int value) => _stateService.SetPreparationTime(value);
    partial void OnTotalEndsChanged(int value) => _stateService.SetTotalEnds(value);
    partial void OnSoundEnabledChanged(bool value) => _soundService.IsEnabled = value;
    partial void OnFinalsSingleDisplayChanged(bool value) => _config.FinalsSingleDisplay = value;
    partial void OnIdleMessageChanged(string value) => _stateService.SetIdleMessage(value);

    public void SaveConfiguration()
    {
        _config.DefaultShootingTime = TimerDuration;
        _config.DefaultPreparationTime = PreparationTime;
        _config.LastGroup = CurrentGroup;
        _config.LastTotalEnds = TotalEnds;
        _config.SoundEnabled = SoundEnabled;
        _config.ComPort = SelectedComPort;
        _config.BeamerMonitor = SelectedScreenIndex;
        _config.FinalsSingleDisplay = FinalsSingleDisplay;
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        _displayRefreshTimer.Stop();
        _stateService.StateChanged -= OnStateChanged;
        _serialService.ConnectionChanged -= OnConnectionChanged;
        _presetEngine.StatusChanged -= OnPresetStatusChanged;
    }
}
