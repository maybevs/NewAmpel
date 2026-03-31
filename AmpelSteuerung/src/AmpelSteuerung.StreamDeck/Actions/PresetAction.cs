using System.Text.Json;
using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Preset selection button. Configurable via Property Inspector.
/// Shows preset name and highlights when the active preset's settings match.
/// </summary>
public class PresetAction : ActionBase
{
    private string _presetName = "";

    public PresetAction(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
        : base(context, connection, apiClient, renderer) { }

    public override Task OnWillAppearAsync(JsonElement? settings)
    {
        base.OnWillAppearAsync(settings);
        _presetName = GetSettingString("presetName");
        return Task.CompletedTask;
    }

    public override Task OnDidReceiveSettingsAsync(JsonElement? settings)
    {
        base.OnDidReceiveSettingsAsync(settings);
        _presetName = GetSettingString("presetName");
        return Task.CompletedTask;
    }

    public override async Task OnKeyDownAsync()
    {
        if (string.IsNullOrEmpty(_presetName))
        {
            await Connection.ShowAlertAsync(Context);
            return;
        }

        var success = await ApiClient.PostCommandAsync($"preset/{Uri.EscapeDataString(_presetName)}");
        if (success)
            await Connection.ShowOkAsync(Context);
        else
            await Connection.ShowAlertAsync(Context);
    }

    protected override async Task RenderKeyAsync(AmpelStateDto state)
    {
        var displayName = string.IsNullOrEmpty(_presetName) ? "Preset" : _presetName;
        var image = Renderer.RenderPresetButton(displayName);
        await SetImageAsync(image);
    }
}
