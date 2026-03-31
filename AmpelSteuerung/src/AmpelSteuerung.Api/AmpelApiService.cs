using AmpelSteuerung.Api.Endpoints;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Api;

public class AmpelApiService : IHostedService
{
    private readonly IAmpelStateService _stateService;
    private readonly PresetEngine _presetEngine;
    private readonly AmpelConfiguration _config;
    private readonly ILogger<AmpelApiService> _logger;
    private WebApplication? _app;

    public AmpelApiService(
        IAmpelStateService stateService,
        PresetEngine presetEngine,
        AmpelConfiguration config,
        ILogger<AmpelApiService> logger)
    {
        _stateService = stateService;
        _presetEngine = presetEngine;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var port = _config.ApiPort;
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = ["--urls", $"http://0.0.0.0:{port}"]
            });

            // Suppress ASP.NET Core startup logs in WPF context
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Register CORS services
            builder.Services.AddCors();

            _app = builder.Build();

            // CORS
            _app.UseCors(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // Map API endpoints
            _app.MapAmpelEndpoints(_stateService, _config, _presetEngine);

            // Serve static HTML
            _app.MapGet("/", () => Results.Content(WebUiHtml.GetHtml(_config.ApiPort), "text/html"));

            _logger.LogInformation("REST API starting on http://0.0.0.0:{Port}", _config.ApiPort);
            await _app.StartAsync(cancellationToken);
            _logger.LogInformation("REST API started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start REST API");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _logger.LogInformation("REST API stopped");
        }
    }
}
