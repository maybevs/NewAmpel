using AmpelSteuerung.Core.Models;
using AmpelSteuerung.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AmpelSteuerung.Api.Endpoints;

public static class AmpelEndpoints
{
    public static void MapAmpelEndpoints(this IEndpointRouteBuilder app, IAmpelStateService stateService)
    {
        app.MapGet("/api/state", () =>
        {
            var state = stateService.CurrentState;
            return Results.Ok(new
            {
                timeRemaining = state.TimeRemaining,
                group = state.Group,
                color = state.Color.ToString().ToLower(),
                end = state.End,
                status = state.Status.ToString().ToLower()
            });
        });

        app.MapPost("/api/start", () =>
        {
            stateService.Start();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/stop", () =>
        {
            stateService.Stop();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/pause", () =>
        {
            stateService.Pause();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/resume", () =>
        {
            stateService.Resume();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/reset", () =>
        {
            stateService.Reset();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/group/{group}", (string group) =>
        {
            stateService.SetGroup(group);
            return Results.Ok(new { success = true, group });
        });

        app.MapPost("/api/duration/{seconds:int}", (int seconds) =>
        {
            if (seconds < 1 || seconds > 999) return Results.BadRequest("Duration must be between 1 and 999");
            stateService.SetDuration(seconds);
            return Results.Ok(new { success = true, duration = seconds });
        });

        app.MapPost("/api/next-end", () =>
        {
            stateService.NextEnd();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/prev-end", () =>
        {
            stateService.PreviousEnd();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/color/{color}", (string color) =>
        {
            var ampelColor = color.ToLower() switch
            {
                "red" => AmpelColor.Red,
                "green" => AmpelColor.Green,
                "yellow" => AmpelColor.Yellow,
                _ => (AmpelColor?)null
            };

            if (ampelColor == null) return Results.BadRequest("Invalid color. Use: red, green, yellow");

            stateService.SetColor(ampelColor.Value);
            return Results.Ok(new { success = true, color });
        });
    }
}
