using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class SoundService : ISoundService
{
    private readonly ILogger<SoundService> _logger;
    private readonly string? _soundFilePath;

    public bool IsEnabled { get; set; } = true;
    public int Volume { get; set; } = 100;

    public SoundService(ILogger<SoundService> logger)
    {
        _logger = logger;
        var candidate = Path.Combine(AppContext.BaseDirectory, "Resources", "Sounds", "horn_signal.wav");
        _soundFilePath = File.Exists(candidate) ? candidate : null;
        if (_soundFilePath == null)
            _logger.LogWarning("Sound file not found at {Path} — falling back to Console.Beep", candidate);
    }

    public void PlayPreparation()
    {
        if (!IsEnabled) return;
        PlayHornBlasts(2);
        _logger.LogDebug("Sound: Preparation (2 horn blasts)");
    }

    public void PlayShootingStart()
    {
        if (!IsEnabled) return;
        PlayHornBlasts(1);
        _logger.LogDebug("Sound: Shooting start (1 horn blast)");
    }

    public void PlayEndCompleted()
    {
        if (!IsEnabled) return;
        PlayHornBlasts(3);
        _logger.LogDebug("Sound: End completed (3 horn blasts)");
    }

    public void PlayEmergencyStop()
    {
        if (!IsEnabled) return;
        PlayHornBlasts(5);
        _logger.LogDebug("Sound: EMERGENCY STOP (5 horn blasts)");
    }

    private void PlayHornBlasts(int count)
    {
        if (!OperatingSystem.IsWindows()) return;
        Task.Run(() =>
        {
            try
            {
                if (_soundFilePath != null)
                    PlayWavWindows(count);
                else
                    PlayBeepsWindows(count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error playing sound");
            }
        });
    }

    [SupportedOSPlatform("windows")]
    private void PlayWavWindows(int count)
    {
        const int pauseMs = 200;
        for (int i = 0; i < count; i++)
        {
            PlaySound(_soundFilePath, IntPtr.Zero, SND_FILENAME | SND_SYNC | SND_NODEFAULT);
            if (i < count - 1)
                Thread.Sleep(pauseMs);
        }
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

    [SupportedOSPlatform("windows")]
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_SYNC = 0x0000;
    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_NODEFAULT = 0x0002;
}
