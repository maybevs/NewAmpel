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
                display1 = new
                {
                    timeRemaining = state.Display1.TimeRemaining,
                    group = state.Display1.Group,
                    color = state.Display1.Color.ToString().ToLower(),
                    end = state.Display1.End
                },
                display2 = new
                {
                    timeRemaining = state.Display2.TimeRemaining,
                    group = state.Display2.Group,
                    color = state.Display2.Color.ToString().ToLower(),
                    end = state.Display2.End
                },
                status = state.Status.ToString().ToLower(),
                phase = state.Phase.ToString(),
                mode = state.Mode.ToString().ToLower(),
                currentEnd = state.End,
                currentSide = state.CurrentSide,
                arrowCountLeft = state.ArrowCountLeft,
                arrowCountRight = state.ArrowCountRight
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

        app.MapPost("/api/skip", () =>
        {
            stateService.Skip();
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/emergency-stop", () =>
        {
            stateService.EmergencyStop();
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

        app.MapPost("/api/preparation/{seconds:int}", (int seconds) =>
        {
            if (seconds < 1 || seconds > 120) return Results.BadRequest("Preparation time must be between 1 and 120");
            stateService.SetPreparationTime(seconds);
            return Results.Ok(new { success = true, preparationTime = seconds });
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

        app.MapPost("/api/start-side/{side}", (string side) =>
        {
            if (side is not ("left" or "right")) return Results.BadRequest("Side must be 'left' or 'right'");
            stateService.SetStartingSide(side);
            return Results.Ok(new { success = true, side });
        });

        app.MapPost("/api/switch-side", () =>
        {
            stateService.SwitchSide();
            return Results.Ok(new { success = true });
        });
    }
}
