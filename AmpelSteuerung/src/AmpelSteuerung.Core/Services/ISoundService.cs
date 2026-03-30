namespace AmpelSteuerung.Core.Services;

public interface ISoundService
{
    bool IsEnabled { get; set; }
    int Volume { get; set; }

    void PlayTimerStart();
    void PlayWarning();
    void PlayTimerEnd();
}
