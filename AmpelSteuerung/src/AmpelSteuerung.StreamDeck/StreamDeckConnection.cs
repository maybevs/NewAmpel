using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AmpelSteuerung.StreamDeck.Models;

namespace AmpelSteuerung.StreamDeck;

public class StreamDeckConnection : IDisposable
{
    private ClientWebSocket? _ws;
    private readonly int _port;
    private readonly string _pluginUUID;
    private readonly string _registerEvent;
    private CancellationToken _ct;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public event EventHandler<StreamDeckEvent>? EventReceived;
    public event EventHandler<JsonElement>? GlobalSettingsReceived;

    public StreamDeckConnection(int port, string pluginUUID, string registerEvent)
    {
        _port = port;
        _pluginUUID = pluginUUID;
        _registerEvent = registerEvent;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        _ct = ct;
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri($"ws://localhost:{_port}"), ct);

        // Register with Stream Deck
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = _registerEvent,
            uuid = _pluginUUID
        }));

        // Start receive loop
        _ = ReceiveLoopAsync(ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];
        using var ms = new MemoryStream();

        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            try
            {
                ms.SetLength(0);
                WebSocketReceiveResult result;

                do
                {
                    result = await _ws.ReceiveAsync(buffer, ct);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    var evt = JsonSerializer.Deserialize<StreamDeckEvent>(json);
                    if (evt != null)
                    {
                        // Handle global settings internally
                        if (evt.Event == "didReceiveGlobalSettings" && evt.Payload.HasValue)
                        {
                            if (evt.Payload.Value.TryGetProperty("settings", out var settings))
                                GlobalSettingsReceived?.Invoke(this, settings);
                        }

                        EventReceived?.Invoke(this, evt);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }
    }

    public async Task SendAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        await SendRawAsync(json);
    }

    private async Task SendRawAsync(string json)
    {
        if (_ws?.State != WebSocketState.Open) return;

        await _sendLock.WaitAsync(_ct);
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task SetImageAsync(string context, string base64DataUri)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "setImage",
            context,
            payload = new { image = base64DataUri, target = 0 }
        }));
    }

    public async Task SetTitleAsync(string context, string title)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "setTitle",
            context,
            payload = new { title, target = 0 }
        }));
    }

    public async Task SetStateAsync(string context, int state)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "setState",
            context,
            payload = new { state }
        }));
    }

    public async Task ShowAlertAsync(string context)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "showAlert",
            context
        }));
    }

    public async Task ShowOkAsync(string context)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "showOk",
            context
        }));
    }

    public async Task GetGlobalSettingsAsync()
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "getGlobalSettings",
            context = _pluginUUID
        }));
    }

    public async Task SetGlobalSettingsAsync(object settings)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "setGlobalSettings",
            context = _pluginUUID,
            payload = settings
        }));
    }

    public async Task SendToPropertyInspectorAsync(string action, string context, object payload)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "sendToPropertyInspector",
            action,
            context,
            payload
        }));
    }

    public async Task GetSettingsAsync(string context)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "getSettings",
            context
        }));
    }

    public async Task LogMessageAsync(string message)
    {
        await SendRawAsync(JsonSerializer.Serialize(new
        {
            @event = "logMessage",
            payload = new { message }
        }));
    }

    public void Dispose()
    {
        _ws?.Dispose();
        _sendLock.Dispose();
    }
}
