using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Displays the current timer with color-coded background.
/// Shows MM:SS countdown, group, end, and phase info.
/// Tapping acts as Start/Pause toggle.
/// </summary>
public class TimerDisplayAction : ActionBase
{
    public TimerDisplayAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
        : base(context, connection, apiClient, renderer) { }

    public override async Task OnKeyDownAsync()
    {
        var state = LastState;
        if (state == null) return;

        // Act as Start/Pause toggle
        if (state.IsStopped || state.IsIdle || state.IsEndCompleted)
        {
            if (state.IsEndCompleted)
                await ApiClient.PostCommandAsync("next-end");
            await ApiClient.PostCommandAsync("start");
        }
        else if (state.IsRunning)
        {
            await ApiClient.PostCommandAsync("pause");
        }
        else if (state.IsPaused && !state.IsEmergencyStopped)
        {
            await ApiClient.PostCommandAsync("resume");
        }
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        // Determine which display to show (configured via settings, default: display1)
        var display = GetSettingString("display", "1") == "2" ? state.Display2 : state.Display1;

        var image = Renderer.RenderTimerDisplay(
            timeRemaining: display.TimeRemaining,
            color: display.Color,
            group: display.Group,
            end: display.End,
            phase: state.Phase,
            status: state.Status,
            isFinalMode: state.IsFinalMode
        );

        await SetImageAsync(image);
    }
}
