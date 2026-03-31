using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Start Side selection button (Links / Rechts) for Final mode.
/// Highlighted when the current starting side matches. Also shows a
/// "Switch Side" action when configured as such.
/// </summary>
public class StartSideAction : ActionBase
{
    private readonly string _side; // "left" or "right"

    public StartSideAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer, string side)
        : base(context, connection, apiClient, renderer)
    {
        _side = side;
    }

    public override async Task OnKeyDownAsync()
    {
        await ApiClient.PostCommandAsync($"start-side/{_side}");
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        bool isActive = state.CurrentSide.Equals(_side, StringComparison.OrdinalIgnoreCase);
        bool isFinal = state.IsFinalMode;

        var image = Renderer.RenderStartSideButton(
            side: _side,
            isActive: isActive,
            isFinalMode: isFinal);

        await SetImageAsync(image);
    }
}
