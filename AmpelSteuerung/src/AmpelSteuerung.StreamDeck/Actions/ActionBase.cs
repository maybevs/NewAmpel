using System.Text.Json;
using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Base class for all Stream Deck actions. Handles context tracking and provides
/// common methods for updating key appearance.
/// </summary>
public abstract class ActionBase
{
    protected StreamDeckConnection Connection { get; }
    protected AmpelApiClient ApiClient { get; }
    protected KeyImageRenderer Renderer { get; }

    /// <summary>Context ID assigned by Stream Deck for this key instance.</summary>
    public string Context { get; }

    /// <summary>Per-instance settings from Stream Deck.</summary>
    public JsonElement? Settings { get; set; }

    protected AmpelStateDto? LastState { get; private set; }

    protected ActionBase(string context, StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
    {
        Context = context;
        Connection = connection;
        ApiClient = apiClient;
        Renderer = renderer;
    }

    /// <summary>Called when the key is pressed down.</summary>
    public virtual Task OnKeyDownAsync() => Task.CompletedTask;

    /// <summary>Called when the key is released.</summary>
    public virtual Task OnKeyUpAsync() => Task.CompletedTask;

    /// <summary>Called when the action first appears on the Stream Deck.</summary>
    public virtual Task OnWillAppearAsync(JsonElement? settings) 
    {
        Settings = settings;
        return Task.CompletedTask;
    }

    /// <summary>Called when the action disappears from the Stream Deck.</summary>
    public virtual Task OnWillDisappearAsync() => Task.CompletedTask;

    /// <summary>Called when settings are updated from the Property Inspector.</summary>
    public virtual Task OnDidReceiveSettingsAsync(JsonElement? settings)
    {
        Settings = settings;
        return Task.CompletedTask;
    }

    /// <summary>Called when a message arrives from the Property Inspector.</summary>
    public virtual Task OnSendToPluginAsync(JsonElement? payload) => Task.CompletedTask;

    /// <summary>Called when the Ampel state changes.</summary>
    public async Task UpdateStateAsync(AmpelStateDto state)
    {
        LastState = state;
        await RenderKeyAsync(state);
    }

    /// <summary>Render the key image based on the current state. Override in subclasses.</summary>
    protected abstract Task RenderKeyAsync(AmpelStateDto state);

    protected async Task SetImageAsync(string base64DataUri)
    {
        await Connection.SetImageAsync(Context, base64DataUri);
    }

    protected string GetSettingString(string key, string defaultValue = "")
    {
        if (Settings == null) return defaultValue;
        if (Settings.Value.TryGetProperty("settings", out var settingsObj))
        {
            if (settingsObj.TryGetProperty(key, out var val))
                return val.GetString() ?? defaultValue;
        }
        // Also check directly on the element (some events nest differently)
        if (Settings.Value.TryGetProperty(key, out var directVal))
            return directVal.GetString() ?? defaultValue;
        return defaultValue;
    }
}
