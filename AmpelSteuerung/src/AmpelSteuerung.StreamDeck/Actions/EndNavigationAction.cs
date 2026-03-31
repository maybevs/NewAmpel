using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// End navigation button (Next End / Previous End).
/// Shows current end counter and navigation direction.
/// </summary>
public class EndNavigationAction : ActionBase
{
    private readonly string _direction; // "next" or "previous"

    public EndNavigationAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer, string direction)
        : base(context, connection, apiClient, renderer)
    {
        _direction = direction;
    }

    public override async Task OnKeyDownAsync()
    {
        var endpoint = _direction == "next" ? "next-end" : "prev-end";
        await ApiClient.PostCommandAsync(endpoint);
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        bool enabled = state.IsStopped || state.IsIdle || state.IsEndCompleted;

        var image = Renderer.RenderEndNavigationButton(
            direction: _direction,
            currentEnd: state.CurrentEnd,
            enabled: enabled);

        await SetImageAsync(image);
    }
}
