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

    // Timer display
    [ObservableProperty] private int _timeRemaining;
    [ObservableProperty] private string _timeDisplay = "02:00";
    [ObservableProperty] private string _currentGroup = "AB";
    [ObservableProperty] private string _currentEnd = "1/10";
    [ObservableProperty] private AmpelColor _currentColor = AmpelColor.Red;
    [ObservableProperty] private TimerStatus _timerStatus = TimerStatus.Stopped;
    [ObservableProperty] private string _colorBrush = "#CC0000";

    // Serial
    [ObservableProperty] private string _selectedComPort = "";
    [ObservableProperty] private bool _isSerialConnected;
    [ObservableProperty] private string _connectionStatusText = "Getrennt";
    [ObservableProperty] private string? _serialError;
    [ObservableProperty] private ObservableCollection<string> _availableComPorts = new();

    // Configuration
    [ObservableProperty] private int _timerDuration = 120;
    [ObservableProperty] private int _totalEnds = 10;
    [ObservableProperty] private bool _soundEnabled = true;

    // Presets
    [ObservableProperty] private ObservableCollection<Preset> _presets = new();
    [ObservableProperty] private Preset? _selectedPreset;
    [ObservableProperty] private string _presetStatus = "";
    [ObservableProperty] private bool _isPresetRunning;

    // UI state
    [ObservableProperty] private bool _isStartEnabled = true;
    [ObservableProperty] private bool _isPauseEnabled;
    [ObservableProperty] private bool _isResumeEnabled;

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

        // Initialize from config
        _timerDuration = config.DefaultDurationSeconds;
        _totalEnds = config.LastTotalEnds;
        _soundEnabled = config.SoundEnabled;
        _soundService.IsEnabled = config.SoundEnabled;
        _soundService.Volume = config.SoundVolume;

        // Wire up events
        _stateService.StateChanged += OnStateChanged;
        _serialService.ConnectionChanged += OnConnectionChanged;
        _presetEngine.StatusChanged += OnPresetStatusChanged;

        // Load presets
        _presetEngine.LoadPresets(config.PresetsFile);
        foreach (var p in _presetEngine.AvailablePresets)
            Presets.Add(p);

        // Update initial state
        UpdateFromState(_stateService.CurrentState);
        RefreshComPorts();
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
        _dispatcher.BeginInvoke(() =>
        {
            PresetStatus = status;
            IsPresetRunning = _presetEngine.IsRunning;
        });
    }

    private void UpdateFromState(AmpelState state)
    {
        TimeRemaining = state.TimeRemaining;
        TimeDisplay = $"{state.TimeRemaining / 60:D2}:{state.TimeRemaining % 60:D2}";
        CurrentGroup = state.Group;
        CurrentEnd = state.End;
        CurrentColor = state.Color;
        TimerStatus = state.Status;

        ColorBrush = state.Color switch
        {
            AmpelColor.Red => "#CC0000",
            AmpelColor.Green => "#00AA00",
            AmpelColor.Yellow => "#DDAA00",
            _ => "#CC0000"
        };

        IsStartEnabled = state.Status == Core.Models.TimerStatus.Stopped;
        IsPauseEnabled = state.Status == Core.Models.TimerStatus.Running;
        IsResumeEnabled = state.Status == Core.Models.TimerStatus.Paused;
    }

    // Commands
    [RelayCommand]
    private void StartTimer()
    {
        _stateService.Start();
    }

    [RelayCommand]
    private void PauseTimer()
    {
        _stateService.Pause();
    }

    [RelayCommand]
    private void ResumeTimer()
    {
        _stateService.Resume();
    }

    [RelayCommand]
    private void StopTimer()
    {
        _stateService.Stop();
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _stateService.Reset();
    }

    [RelayCommand]
    private void ToggleStartPause()
    {
        var state = _stateService.CurrentState;
        switch (state.Status)
        {
            case Core.Models.TimerStatus.Stopped:
                _stateService.Start();
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
    private void NextEnd() => _stateService.NextEnd();

    [RelayCommand]
    private void PreviousEnd() => _stateService.PreviousEnd();

    [RelayCommand]
    private void SetColorRed() => _stateService.SetColor(AmpelColor.Red);

    [RelayCommand]
    private void SetColorGreen() => _stateService.SetColor(AmpelColor.Green);

    [RelayCommand]
    private void SetColorYellow() => _stateService.SetColor(AmpelColor.Yellow);

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

    [RelayCommand]
    private async Task StartPresetAsync()
    {
        if (SelectedPreset == null) return;
        await _presetEngine.StartPresetAsync(SelectedPreset);
    }

    [RelayCommand]
    private void StopPreset()
    {
        _presetEngine.StopPreset();
    }

    partial void OnTimerDurationChanged(int value)
    {
        _stateService.SetDuration(value);
    }

    partial void OnTotalEndsChanged(int value)
    {
        _stateService.SetTotalEnds(value);
    }

    partial void OnSoundEnabledChanged(bool value)
    {
        _soundService.IsEnabled = value;
    }

    public void SaveConfiguration()
    {
        _config.DefaultDurationSeconds = TimerDuration;
        _config.LastGroup = CurrentGroup;
        _config.LastTotalEnds = TotalEnds;
        _config.SoundEnabled = SoundEnabled;
        _config.ComPort = SelectedComPort;
    }

    public void Dispose()
    {
        _stateService.StateChanged -= OnStateChanged;
        _serialService.ConnectionChanged -= OnConnectionChanged;
        _presetEngine.StatusChanged -= OnPresetStatusChanged;
    }
}
