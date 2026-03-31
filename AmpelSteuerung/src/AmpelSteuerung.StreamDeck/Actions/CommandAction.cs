using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Generic command button for simple actions (Stop, Reset).
/// Appearance adapts to current state.
/// </summary>
public class CommandAction : ActionBase
{
    private readonly string _command;
    private readonly ButtonStyle _style;

    public enum ButtonStyle { Stop, Reset, SwitchSide }

    public CommandAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer,
        string command, ButtonStyle style)
        : base(context, connection, apiClient, renderer)
    {
        _command = command;
        _style = style;
    }

    public override async Task OnKeyDownAsync()
    {
        await ApiClient.PostCommandAsync(_command);
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        switch (_style)
        {
            case ButtonStyle.Stop:
            {
                bool active = state.IsRunning || state.IsPaused;
                var image = Renderer.RenderActionButton(
                    "Stop",
                    active ? "#E74C3C" : "#4A1A1A",
                    "stop",
                    enabled: active);
                await SetImageAsync(image);
                break;
            }
            case ButtonStyle.Reset:
            {
                bool active = state.IsStopped || state.IsIdle || state.IsEndCompleted;
                var image = Renderer.RenderActionButton(
                    "Reset",
                    active ? "#7F8C8D" : "#3D4145",
                    "reset",
                    enabled: active);
                await SetImageAsync(image);
                break;
            }
            case ButtonStyle.SwitchSide:
            {
                bool active = state.IsFinalMode;
                var label = state.CurrentSide == "left" ? "L \u2194 R" : "R \u2194 L";
                var image = Renderer.RenderActionButton(
                    label,
                    active ? "#3498DB" : "#1A2A3A",
                    "switch",
                    enabled: active);
                await SetImageAsync(image);
                break;
            }
        }
    }
}
