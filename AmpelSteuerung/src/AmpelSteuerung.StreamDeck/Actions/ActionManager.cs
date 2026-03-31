using System.Text.Json;
using AmpelSteuerung.StreamDeck.Models;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck.Actions;

/// <summary>
/// Routes Stream Deck events to the appropriate action instances.
/// Manages action lifecycle (creation, tracking, disposal).
/// </summary>
public class ActionManager
{
    private readonly StreamDeckConnection _connection;
    private readonly AmpelApiClient _apiClient;
    private readonly KeyImageRenderer _renderer;

    // context → action instance
    private readonly Dictionary<string, ActionBase> _actions = new();
    private readonly object _lock = new();

    public ActionManager(StreamDeckConnection connection, AmpelApiClient apiClient, KeyImageRenderer renderer)
    {
        _connection = connection;
        _apiClient = apiClient;
        _renderer = renderer;
    }

    public async Task HandleEventAsync(StreamDeckEvent evt)
    {
        switch (evt.Event)
        {
            case "willAppear":
                await HandleWillAppearAsync(evt);
                break;

            case "willDisappear":
                await HandleWillDisappearAsync(evt);
                break;

            case "keyDown":
                if (evt.Context != null && TryGetAction(evt.Context, out var keyAction))
                    await keyAction.OnKeyDownAsync();
                break;

            case "keyUp":
                if (evt.Context != null && TryGetAction(evt.Context, out var upAction))
                    await upAction.OnKeyUpAsync();
                break;

            case "didReceiveSettings":
                if (evt.Context != null && TryGetAction(evt.Context, out var settingsAction))
                    await settingsAction.OnDidReceiveSettingsAsync(evt.Payload);
                break;

            case "sendToPlugin":
                if (evt.Context != null && TryGetAction(evt.Context, out var piAction))
                    await piAction.OnSendToPluginAsync(evt.Payload);
                break;

            case "propertyInspectorDidAppear":
                if (evt.Context != null && evt.Action != null)
                    await HandlePropertyInspectorAppearAsync(evt);
                break;
        }
    }

    /// <summary>
    /// Push the latest Ampel state to all active action instances.
    /// </summary>
    public async Task UpdateStateAsync(AmpelStateDto state)
    {
        ActionBase[] actions;
        lock (_lock)
        {
            actions = _actions.Values.ToArray();
        }

        foreach (var action in actions)
        {
            try
            {
                await action.UpdateStateAsync(state);
            }
            catch { /* Don't let one action failure stop others */ }
        }
    }

    private async Task HandleWillAppearAsync(StreamDeckEvent evt)
    {
        if (evt.Context == null || evt.Action == null) return;

        var action = CreateAction(evt.Action, evt.Context);
        if (action == null) return;

        lock (_lock)
        {
            _actions[evt.Context] = action;
        }

        await action.OnWillAppearAsync(evt.Payload);
    }

    private async Task HandleWillDisappearAsync(StreamDeckEvent evt)
    {
        if (evt.Context == null) return;

        ActionBase? action;
        lock (_lock)
        {
            _actions.Remove(evt.Context, out action);
        }

        if (action != null)
            await action.OnWillDisappearAsync();
    }

    private async Task HandlePropertyInspectorAppearAsync(StreamDeckEvent evt)
    {
        // When PI opens for preset action, send available presets
        if (evt.Action == ActionUUIDs.Preset && evt.Context != null)
        {
            var presets = await _apiClient.GetPresetsAsync();
            if (presets != null)
            {
                await _connection.SendToPropertyInspectorAsync(evt.Action, evt.Context, new
                {
                    type = "presets",
                    presets
                });
            }
        }
    }

    private ActionBase? CreateAction(string actionUUID, string context)
    {
        return actionUUID switch
        {
            ActionUUIDs.TimerDisplay => new TimerDisplayAction(context, _connection, _apiClient, _renderer),
            ActionUUIDs.StartPause => new StartPauseAction(context, _connection, _apiClient, _renderer),
            ActionUUIDs.Stop => new CommandAction(context, _connection, _apiClient, _renderer, "stop", CommandAction.ButtonStyle.Stop),
            ActionUUIDs.Reset => new CommandAction(context, _connection, _apiClient, _renderer, "reset", CommandAction.ButtonStyle.Reset),
            ActionUUIDs.Skip => new SkipAction(context, _connection, _apiClient, _renderer),
            ActionUUIDs.EmergencyStop => new EmergencyStopAction(context, _connection, _apiClient, _renderer),
            ActionUUIDs.NextEnd => new EndNavigationAction(context, _connection, _apiClient, _renderer, "next"),
            ActionUUIDs.PreviousEnd => new EndNavigationAction(context, _connection, _apiClient, _renderer, "previous"),
            ActionUUIDs.GroupAB => new GroupSelectAction(context, _connection, _apiClient, _renderer, "AB"),
            ActionUUIDs.GroupCD => new GroupSelectAction(context, _connection, _apiClient, _renderer, "CD"),
            ActionUUIDs.Preset => new PresetAction(context, _connection, _apiClient, _renderer),
            ActionUUIDs.StartSideLeft => new StartSideAction(context, _connection, _apiClient, _renderer, "left"),
            ActionUUIDs.StartSideRight => new StartSideAction(context, _connection, _apiClient, _renderer, "right"),
            ActionUUIDs.SwitchSide => new CommandAction(context, _connection, _apiClient, _renderer, "switch-side", CommandAction.ButtonStyle.SwitchSide),
            _ => null
        };
    }

    private bool TryGetAction(string context, out ActionBase action)
    {
        lock (_lock)
        {
            return _actions.TryGetValue(context, out action!);
        }
    }
}

/// <summary>
/// Action UUID constants matching manifest.json definitions.
/// </summary>
public static class ActionUUIDs
{
    public const string Prefix = "com.ampelsteuerung.";
    public const string TimerDisplay = Prefix + "timer-display";
    public const string StartPause = Prefix + "start-pause";
    public const string Stop = Prefix + "stop";
    public const string Reset = Prefix + "reset";
    public const string Skip = Prefix + "skip";
    public const string EmergencyStop = Prefix + "emergency-stop";
    public const string NextEnd = Prefix + "next-end";
    public const string PreviousEnd = Prefix + "prev-end";
    public const string GroupAB = Prefix + "group-ab";
    public const string GroupCD = Prefix + "group-cd";
    public const string Preset = Prefix + "preset";
    public const string StartSideLeft = Prefix + "start-side-left";
    public const string StartSideRight = Prefix + "start-side-right";
    public const string SwitchSide = Prefix + "switch-side";
}
