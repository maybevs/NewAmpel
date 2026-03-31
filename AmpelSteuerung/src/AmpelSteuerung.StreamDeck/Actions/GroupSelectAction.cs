using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Group selection button (AB or CD).
/// Highlighted when the current group matches this button's group.
/// </summary>
public class GroupSelectAction : ActionBase
{
    private readonly string _group;

    public GroupSelectAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer, string group)
        : base(context, connection, apiClient, renderer)
    {
        _group = group;
    }

    public override async Task OnKeyDownAsync()
    {
        await ApiClient.PostCommandAsync($"group/{_group}");
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        bool isActive = state.Display1.Group.Equals(_group, StringComparison.OrdinalIgnoreCase);
        var image = Renderer.RenderGroupButton(_group, isActive);
        await SetImageAsync(image);
    }
}
