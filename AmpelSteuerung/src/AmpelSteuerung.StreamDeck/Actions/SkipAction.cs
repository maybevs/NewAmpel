using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Skip button. Only active during shooting phases.
/// Dimmed when skip is not available.
/// </summary>
public class SkipAction : ActionBase
{
    public SkipAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
        : base(context, connection, apiClient, renderer) { }

    public override async Task OnKeyDownAsync()
    {
        var state = LastState;
        if (state == null) return;

        if (state.IsShooting && state.IsRunning)
        {
            await ApiClient.PostCommandAsync("skip");
            await Connection.ShowOkAsync(Context);
        }
        else
        {
            await Connection.ShowAlertAsync(Context);
        }
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        bool active = state.IsShooting && state.IsRunning;
        var image = Renderer.RenderActionButton(
            "Skip",
            active ? "#E8A030" : "#4A3A1A",
            "skip",
            enabled: active);
        await SetImageAsync(image);
    }
}
