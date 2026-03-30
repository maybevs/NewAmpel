using System.Text.Json;
using AmpelSteuerung.Core.Models;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class PresetEngine
{
    private readonly IAmpelStateService _stateService;
    private readonly ILogger<PresetEngine> _logger;
    private CancellationTokenSource? _cts;
    private Preset? _activePreset;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public Preset? ActivePreset => _activePreset;
    public List<Preset> AvailablePresets { get; private set; } = new();

    public event EventHandler<string>? StatusChanged;

    public PresetEngine(IAmpelStateService stateService, ILogger<PresetEngine> logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    public void LoadPresets(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Presets file not found: {Path}. Creating defaults.", filePath);
                CreateDefaultPresets(filePath);
            }

            var json = File.ReadAllText(filePath);
            AvailablePresets = JsonSerializer.Deserialize<List<Preset>>(json) ?? new List<Preset>();
            _logger.LogInformation("Loaded {Count} presets from {Path}", AvailablePresets.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading presets from {Path}", filePath);
            AvailablePresets = new List<Preset>();
        }
    }

    public async Task StartPresetAsync(Preset preset)
    {
        StopPreset();

        _activePreset = preset;
        _cts = new CancellationTokenSource();
        _isRunning = true;

        _stateService.SetDuration(preset.TimerDuration);
        _stateService.SetTotalEnds(preset.Ends);

        _logger.LogInformation("Starting preset: {Name}", preset.Name);
        StatusChanged?.Invoke(this, $"Preset gestartet: {preset.Name}");

        try
        {
            await ExecuteSequenceAsync(preset, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Preset cancelled: {Name}", preset.Name);
            StatusChanged?.Invoke(this, "Preset abgebrochen");
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void StopPreset()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _isRunning = false;
    }

    private async Task ExecuteSequenceAsync(Preset preset, CancellationToken ct)
    {
        var endCount = 0;

        while (!ct.IsCancellationRequested)
        {
            foreach (var action in preset.Sequence)
            {
                ct.ThrowIfCancellationRequested();

                switch (action.Action)
                {
                    case "setGroup":
                        if (action.Value != null)
                            _stateService.SetGroup(action.Value);
                        StatusChanged?.Invoke(this, $"Gruppe: {action.Value}");
                        break;

                    case "startTimer":
                        _stateService.Start();
                        StatusChanged?.Invoke(this, "Timer gestartet");
                        break;

                    case "waitForTimerEnd":
                        await WaitForTimerEndAsync(ct);
                        break;

                    case "pauseBetweenGroups":
                        var pauseDuration = action.Duration ?? 30;
                        StatusChanged?.Invoke(this, $"Pause: {pauseDuration}s");
                        await Task.Delay(TimeSpan.FromSeconds(pauseDuration), ct);
                        break;

                    case "nextEnd":
                        _stateService.NextEnd();
                        endCount++;
                        StatusChanged?.Invoke(this, $"Passe {_stateService.Config.CurrentEnd}/{preset.Ends}");

                        if (endCount >= preset.Ends * preset.Groups.Length)
                        {
                            StatusChanged?.Invoke(this, "Preset abgeschlossen");
                            return;
                        }
                        break;

                    case "repeat":
                        // Continue the while loop
                        break;

                    default:
                        _logger.LogWarning("Unknown preset action: {Action}", action.Action);
                        break;
                }
            }
        }
    }

    private async Task WaitForTimerEndAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();

        void OnStateChanged(object? sender, AmpelState state)
        {
            if (state.Status == TimerStatus.Stopped && state.TimeRemaining == 0)
                tcs.TrySetResult();
        }

        _stateService.StateChanged += OnStateChanged;

        try
        {
            using var registration = ct.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }
        finally
        {
            _stateService.StateChanged -= OnStateChanged;
        }
    }

    private void CreateDefaultPresets(string filePath)
    {
        var defaults = new List<Preset>
        {
            new()
            {
                Name = "Wettkampf 70m Outdoor",
                Ends = 12,
                ArrowsPerEnd = 6,
                Groups = ["AB", "CD"],
                TimerDuration = 240,
                WarningTime = 30,
                Sequence =
                [
                    new PresetAction { Action = "setGroup", Value = "AB" },
                    new PresetAction { Action = "startTimer" },
                    new PresetAction { Action = "waitForTimerEnd" },
                    new PresetAction { Action = "pauseBetweenGroups", Duration = 30 },
                    new PresetAction { Action = "setGroup", Value = "CD" },
                    new PresetAction { Action = "startTimer" },
                    new PresetAction { Action = "waitForTimerEnd" },
                    new PresetAction { Action = "nextEnd" },
                    new PresetAction { Action = "repeat" }
                ]
            },
            new()
            {
                Name = "Halle 18m",
                Ends = 10,
                ArrowsPerEnd = 3,
                Groups = ["AB", "CD"],
                TimerDuration = 120,
                WarningTime = 30,
                Sequence =
                [
                    new PresetAction { Action = "setGroup", Value = "AB" },
                    new PresetAction { Action = "startTimer" },
                    new PresetAction { Action = "waitForTimerEnd" },
                    new PresetAction { Action = "pauseBetweenGroups", Duration = 20 },
                    new PresetAction { Action = "setGroup", Value = "CD" },
                    new PresetAction { Action = "startTimer" },
                    new PresetAction { Action = "waitForTimerEnd" },
                    new PresetAction { Action = "nextEnd" },
                    new PresetAction { Action = "repeat" }
                ]
            },
            new()
            {
                Name = "Training",
                Ends = 20,
                ArrowsPerEnd = 3,
                Groups = ["AB"],
                TimerDuration = 120,
                WarningTime = 30,
                Sequence =
                [
                    new PresetAction { Action = "setGroup", Value = "AB" },
                    new PresetAction { Action = "startTimer" },
                    new PresetAction { Action = "waitForTimerEnd" },
                    new PresetAction { Action = "pauseBetweenGroups", Duration = 60 },
                    new PresetAction { Action = "nextEnd" },
                    new PresetAction { Action = "repeat" }
                ]
            }
        };

        var json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, json);
    }
}
