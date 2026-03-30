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

    public void PlayTimerStart()
    {
        if (!IsEnabled) return;
        PlayBeeps(1);
        _logger.LogDebug("Sound: Timer start (1 beep)");
    }

    public void PlayWarning()
    {
        if (!IsEnabled) return;
        PlayBeeps(2);
        _logger.LogDebug("Sound: Warning (2 beeps)");
    }

    public void PlayTimerEnd()
    {
        if (!IsEnabled) return;
        PlayBeeps(3);
        _logger.LogDebug("Sound: Timer end (3 beeps)");
    }

    private void PlayBeeps(int count)
    {
        if (!OperatingSystem.IsWindows()) return;
        // Run on background thread to avoid blocking
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
        var frequency = 1000;
        var duration = 200;
        var pause = 150;

        for (int i = 0; i < count; i++)
        {
            Console.Beep(frequency, duration);
            if (i < count - 1)
                Thread.Sleep(pause);
        }
    }
}
