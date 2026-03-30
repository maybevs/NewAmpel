using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class SoundService : ISoundService
{
    private readonly ILogger<SoundService> _logger;

    public bool IsEnabled { get; set; } = true;
    public int Volume { get; set; } = 100;

    public SoundService(ILogger<SoundService> logger)
    {
        _logger = logger;
    }

    public void PlayPreparation()
    {
        if (!IsEnabled) return;
        PlayBeeps(2);
        _logger.LogDebug("Sound: Preparation (2 horn blasts)");
    }

    public void PlayShootingStart()
    {
        if (!IsEnabled) return;
        PlayBeeps(1);
        _logger.LogDebug("Sound: Shooting start (1 horn blast)");
    }

    public void PlayEndCompleted()
    {
        if (!IsEnabled) return;
        PlayBeeps(3);
        _logger.LogDebug("Sound: End completed (3 horn blasts)");
    }

    public void PlayEmergencyStop()
    {
        if (!IsEnabled) return;
        PlayBeeps(5);
        _logger.LogDebug("Sound: EMERGENCY STOP (5 horn blasts)");
    }

    private void PlayBeeps(int count)
    {
        if (!OperatingSystem.IsWindows()) return;
        Task.Run(() =>
        {
            try
            {
                PlayBeepsWindows(count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error playing beep sound");
            }
        });
    }

    [SupportedOSPlatform("windows")]
    private static void PlayBeepsWindows(int count)
    {
        const int frequency = 800;
        const int duration = 400;
        const int pause = 200;

        for (int i = 0; i < count; i++)
        {
            Console.Beep(frequency, duration);
            if (i < count - 1)
                Thread.Sleep(pause);
        }
    }
}
