using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Emergency Stop button. Always prominently red.
/// Highlighted/pulsing when emergency is already active.
/// </summary>
public class EmergencyStopAction : ActionBase
{
    public EmergencyStopAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
        : base(context, connection, apiClient, renderer) { }

    public override async Task OnKeyDownAsync()
    {
        await ApiClient.PostCommandAsync("emergency-stop");
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        var image = Renderer.RenderEmergencyButton(isActive: state.IsEmergencyStopped);
        await SetImageAsync(image);
    }
}
