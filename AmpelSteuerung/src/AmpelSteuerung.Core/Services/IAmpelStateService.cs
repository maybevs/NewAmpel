using AmpelSteuerung.Core.Models;

namespace AmpelSteuerung.Core.Services;

public interface IAmpelStateService
{
    AmpelState CurrentState { get; }
    TimerConfig Config { get; }
    event EventHandler<AmpelState>? StateChanged;

    void Start();
    void Stop();
    void Pause();
    void Resume();
    void Reset();
    void SetGroup(string group);
    void SetDuration(int seconds);
    void SetColor(AmpelColor color);
    void NextEnd();
    void PreviousEnd();
    void SetTotalEnds(int totalEnds);
}
