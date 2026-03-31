using System.Text.Json;
using AmpelSteuerung.StreamDeck.Actions;
using AmpelSteuerung.StreamDeck.Rendering;

namespace AmpelSteuerung.StreamDeck;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // Generate icons if needed (run with --generate-icons to create them manually)
        if (args.Contains("--generate-icons"))
        {
            var pluginDir = Path.Combine(AppContext.BaseDirectory, "sdplugin");
            Console.WriteLine($"Generating icons in: {pluginDir}");
            IconGenerator.GenerateAll(pluginDir);
            Console.WriteLine("Icons generated successfully.");
            return;
        }

        // Stream Deck passes: -port <PORT> -pluginUUID <UUID> -registerEvent <EVENT> -info <JSON>
        var port = 0;
        var pluginUUID = "";
        var registerEvent = "";
        var info = "";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-port" when i + 1 < args.Length:
                    port = int.Parse(args[++i]);
                    break;
                case "-pluginUUID" when i + 1 < args.Length:
                    pluginUUID = args[++i];
                    break;
                case "-registerEvent" when i + 1 < args.Length:
                    registerEvent = args[++i];
                    break;
                case "-info" when i + 1 < args.Length:
                    info = args[++i];
                    break;
            }
        }

        if (port == 0 || string.IsNullOrEmpty(pluginUUID) || string.IsNullOrEmpty(registerEvent))
        {
            Console.Error.WriteLine("Missing required Stream Deck arguments. This plugin must be launched by Stream Deck.");
            Console.Error.WriteLine("Usage: -port <PORT> -pluginUUID <UUID> -registerEvent <EVENT> -info <JSON>");
            return;
        }

        // Parse API URL from global settings (default: http://localhost:5000)
        var apiUrl = "http://localhost:5000";

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            var connection = new StreamDeckConnection(port, pluginUUID, registerEvent);
            var apiClient = new AmpelApiClient(apiUrl);
            var renderer = new KeyImageRenderer();
            var actionManager = new ActionManager(connection, apiClient, renderer);

            connection.EventReceived += (_, evt) => _ = actionManager.HandleEventAsync(evt);
            connection.GlobalSettingsReceived += (_, settings) =>
            {
                if (settings.TryGetProperty("apiUrl", out var urlProp))
                {
                    var newUrl = urlProp.GetString();
                    if (!string.IsNullOrWhiteSpace(newUrl))
                        apiClient.BaseUrl = newUrl;
                }
            };

            await connection.ConnectAsync(cts.Token);

            // Request global settings to get API URL
            await connection.GetGlobalSettingsAsync();

            // Start polling state and updating keys
            _ = PollStateLoopAsync(apiClient, actionManager, cts.Token);

            // Keep running until cancelled
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
        }
    }

    private static async Task PollStateLoopAsync(AmpelApiClient apiClient, ActionManager actionManager, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var state = await apiClient.GetStateAsync(ct);
                if (state != null)
                {
                    await actionManager.UpdateStateAsync(state);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* Connection lost, retry next cycle */ }

            await Task.Delay(500, ct);
        }
    }
}
