using AmpelSteuerung.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AmpelSteuerung.Core.Services;

public class PresetEngine
{
    private readonly IAmpelStateService _stateService;
    private readonly ILogger<PresetEngine> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public List<Preset> AvailablePresets { get; private set; } = new();

    public event EventHandler<string>? StatusChanged;

    public PresetEngine(IAmpelStateService stateService, ILogger<PresetEngine> logger)
    {
        _stateService = stateService;
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public void LoadPresets(string directory)
    {
        AvailablePresets.Clear();

        try
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Presets directory not found: {Dir}. Creating defaults.", directory);
                Directory.CreateDirectory(directory);
                CreateDefaultPresets(directory);
            }

            foreach (var file in Directory.GetFiles(directory, "*.yaml").Concat(Directory.GetFiles(directory, "*.yml")))
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var preset = _yamlDeserializer.Deserialize<Preset>(yaml);
                    if (preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                    {
                        AvailablePresets.Add(preset);
                        _logger.LogInformation("Loaded preset: {Name} from {File}", preset.Name, Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading preset from {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} presets from {Dir}", AvailablePresets.Count, directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading presets directory {Dir}", directory);
        }
    }

    public void ApplyPreset(Preset preset)
    {
        _stateService.ApplyPreset(preset);
        StatusChanged?.Invoke(this, $"Preset aktiv: {preset.Name}");
        _logger.LogInformation("Applied preset: {Name}", preset.Name);
    }

    private void CreateDefaultPresets(string directory)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var presets = new Dictionary<string, Preset>
        {
            ["wa720.yaml"] = new()
            {
                Name = "WA 720 (70m Outdoor)",
                Description = "6 Pfeile pro Passe, 240 Sekunden Schießzeit, AB/CD Wechsel",
                Type = "standard",
                Timer = new PresetTimerSettings { ShootingTime = 240, PreparationTime = 10, WarningTime = 30 },
                Groups = new PresetGroupSettings { Mode = "alternating", Names = ["AB", "CD"], AlternateStartOrder = true },
                Match = new PresetMatchSettings { TotalEnds = 12, ArrowsPerEnd = 6 },
                Options = new PresetOptions { GroupSwitchEnabled = true, SkipEnabled = true }
            },
            ["wa_indoor.yaml"] = new()
            {
                Name = "WA Indoor (18m)",
                Description = "3 Pfeile pro Passe, 120 Sekunden Schießzeit",
                Type = "standard",
                Timer = new PresetTimerSettings { ShootingTime = 120, PreparationTime = 10, WarningTime = 30 },
                Groups = new PresetGroupSettings { Mode = "alternating", Names = ["AB", "CD"], AlternateStartOrder = true },
                Match = new PresetMatchSettings { TotalEnds = 10, ArrowsPerEnd = 3 },
                Options = new PresetOptions { GroupSwitchEnabled = true, SkipEnabled = true }
            },
            ["training.yaml"] = new()
            {
                Name = "Training",
                Description = "Freies Training, eine Gruppe, 120 Sekunden",
                Type = "standard",
                Timer = new PresetTimerSettings { ShootingTime = 120, PreparationTime = 10, WarningTime = 30 },
                Groups = new PresetGroupSettings { Mode = "single", Names = ["AB"], AlternateStartOrder = false },
                Match = new PresetMatchSettings { TotalEnds = 20, ArrowsPerEnd = 3 },
                Options = new PresetOptions { GroupSwitchEnabled = false, SkipEnabled = true }
            },
            ["final_individual.yaml"] = new()
            {
                Name = "Einzelfinale",
                Description = "Alternierend, 1 Pfeil pro Seite, 20 Sekunden",
                Type = "final",
                Timer = new PresetTimerSettings { ShootingTime = 20, PreparationTime = 10, WarningTime = 5 },
                Groups = new PresetGroupSettings { Mode = "alternating", Names = ["1", "2"] },
                Match = new PresetMatchSettings { TotalEnds = 5, ArrowsPerEnd = 3 },
                Final = new PresetFinalSettings { ArrowsPerSide = 1, TotalArrowsPerEnd = 3, Sides = ["1", "2"], StartSide = "manual", SkipEnabled = true }
            },
            ["final_mixed_team.yaml"] = new()
            {
                Name = "Mixed Team Finale",
                Description = "2 Schützen pro Seite, je 1 Pfeil = 20 Sek. pro Team",
                Type = "final",
                Timer = new PresetTimerSettings { ShootingTime = 20, PreparationTime = 10, WarningTime = 5 },
                Groups = new PresetGroupSettings { Mode = "alternating", Names = ["1", "2"] },
                Match = new PresetMatchSettings { TotalEnds = 4, ArrowsPerEnd = 4 },
                Final = new PresetFinalSettings { ArrowsPerSide = 2, TotalArrowsPerEnd = 4, Sides = ["1", "2"], StartSide = "manual", SkipEnabled = true }
            },
            ["final_team.yaml"] = new()
            {
                Name = "Team Finale (3er Mannschaft)",
                Description = "3 Schützen pro Seite, je 1 Pfeil = 30 Sek. pro Team",
                Type = "final",
                Timer = new PresetTimerSettings { ShootingTime = 30, PreparationTime = 10, WarningTime = 5 },
                Groups = new PresetGroupSettings { Mode = "alternating", Names = ["1", "2"] },
                Match = new PresetMatchSettings { TotalEnds = 4, ArrowsPerEnd = 6 },
                Final = new PresetFinalSettings { ArrowsPerSide = 3, TotalArrowsPerEnd = 6, Sides = ["1", "2"], StartSide = "manual", SkipEnabled = true }
            }
        };

        foreach (var (filename, preset) in presets)
        {
            var yaml = serializer.Serialize(preset);
            File.WriteAllText(Path.Combine(directory, filename), yaml);
        }

        _logger.LogInformation("Created {Count} default preset files in {Dir}", presets.Count, directory);
    }
}
