using AmpelSteuerung.Core.Models;

namespace AmpelSteuerung.Core.Services;

public interface IAmpelStateService
{
    AmpelState CurrentState { get; }
    TimerConfig Config { get; }
    event EventHandler<AmpelState>? StateChanged;

    // Lifecycle
    void Start();
    void Stop();
    void Pause();
    void Resume();
    void Reset();

    // Skip / advance
    void Skip();

    // Emergency
    void EmergencyStop();

    // Configuration
    void SetGroup(string group);
    void SetDuration(int seconds);
    void SetPreparationTime(int seconds);
    void SetColor(AmpelColor color);
    void NextEnd();
    void PreviousEnd();
    void SetTotalEnds(int totalEnds);
    void SetStartingGroup(int groupIndex);

    // Final mode
    void SetStartingSide(string side);
    void SwitchSide();

    // Mode / Preset
    void ApplyPreset(Preset preset);
    void SetMode(OperatingMode mode);

    // Display mapping
    string Display1Side { get; set; }
    string Display2Side { get; set; }
}
