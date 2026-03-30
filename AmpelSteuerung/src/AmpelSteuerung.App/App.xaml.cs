using System.IO;
using System.Text.Json;
using System.Windows;
using AmpelSteuerung.Api;
using AmpelSteuerung.App.ViewModels;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AmpelSteuerung.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load or create configuration
        var config = LoadConfiguration();

        // Build host with DI
        _host = Host.CreateDefaultBuilder()
            .UseSerilog((context, loggerConfig) =>
            {
                loggerConfig
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("logs/ampel-.log", rollingInterval: RollingInterval.Day);
            })
            .ConfigureServices(services =>
            {
                // Configuration
                services.AddSingleton(config);

                // Core services
                services.AddSingleton<ISoundService, SoundService>();
                services.AddSingleton<IAmpelStateService, AmpelStateService>();
                services.AddSingleton<ISerialService, SerialService>();
                services.AddSingleton<IStreamDeckService, StreamDeckService>();
                services.AddSingleton<PresetEngine>();

                // API
                services.AddHostedService<AmpelApiService>();

                // ViewModel
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var viewModel = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.SetViewModel(viewModel);

        // Restore window position
        if (config.WindowLeft >= 0 && config.WindowTop >= 0)
        {
            mainWindow.Left = config.WindowLeft;
            mainWindow.Top = config.WindowTop;
        }
        mainWindow.Width = config.WindowWidth;
        mainWindow.Height = config.WindowHeight;

        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            // Save configuration
            var config = _host.Services.GetRequiredService<AmpelConfiguration>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            config.WindowLeft = mainWindow.Left;
            config.WindowTop = mainWindow.Top;
            config.WindowWidth = mainWindow.Width;
            config.WindowHeight = mainWindow.Height;
            SaveConfiguration(config);

            // Cleanup
            var serial = _host.Services.GetRequiredService<ISerialService>();
            serial.Dispose();

            var stateService = _host.Services.GetRequiredService<IAmpelStateService>();
            if (stateService is IDisposable disposable) disposable.Dispose();

            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static AmpelConfiguration LoadConfiguration()
    {
        const string configFile = "ampel-config.json";
        try
        {
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                return JsonSerializer.Deserialize<AmpelConfiguration>(json) ?? new AmpelConfiguration();
            }
        }
        catch { }
        return new AmpelConfiguration();
    }

    private static void SaveConfiguration(AmpelConfiguration config)
    {
        const string configFile = "ampel-config.json";
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, json);
        }
        catch { }
    }
}
