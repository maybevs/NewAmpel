namespace AmpelSteuerung.Core.Services;

public interface ISoundService
{
    bool IsEnabled { get; set; }
    int Volume { get; set; }

    /// <summary>2 horn blasts — preparation begins (Schützen betreten die Schießlinie)</summary>
    void PlayPreparation();

    /// <summary>1 horn blast — shooting begins (Ampel Grün)</summary>
    void PlayShootingStart();

    /// <summary>3 horn blasts — end completed / scoring (Bögen ablegen)</summary>
    void PlayEndCompleted();

    /// <summary>5 horn blasts — emergency stop (sofortiger Stopp)</summary>
    void PlayEmergencyStop();
}
