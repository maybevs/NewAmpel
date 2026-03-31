using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Multi-state Start/Pause/Resume button.
/// Adapts label and appearance based on current timer status.
/// </summary>
public class StartPauseAction : ActionBase
{
    public StartPauseAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
        : base(context, connection, apiClient, renderer) { }

    public override async Task OnKeyDownAsync()
    {
        var state = LastState;
        if (state == null) return;

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
        string label;
        string bgColor;
        string icon;

        if (state.IsEmergencyStopped)
        {
            label = "GESPERRT";
            bgColor = "#555555";
            icon = "blocked";
        }
        else if (state.IsRunning)
        {
            label = "Pause";
            bgColor = "#3498DB"; // Blue
            icon = "pause";
        }
        else if (state.IsPaused)
        {
            label = "Weiter";
            bgColor = "#E67E22"; // Orange
            icon = "play";
        }
        else if (state.IsEndCompleted)
        {
            label = "Nächste";
            bgColor = "#2ECC71"; // Green
            icon = "next";
        }
        else
        {
            label = "Start";
            bgColor = "#2ECC71"; // Green
            icon = "play";
        }

        var image = Renderer.RenderActionButton(label, bgColor, icon, enabled: !state.IsEmergencyStopped);
        await SetImageAsync(image);
    }
}
