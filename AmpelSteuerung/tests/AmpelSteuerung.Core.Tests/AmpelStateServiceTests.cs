using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Models;
using AmpelSteuerung.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AmpelSteuerung.Core.Tests;

public class AmpelStateServiceTests
{
    private static AmpelStateService CreateService()
    {
        var config = new AmpelConfiguration { DefaultDurationSeconds = 10, WarningTimeSeconds = 3 };
        var soundService = new FakeSoundService();
        var logger = NullLogger<AmpelStateService>.Instance;
        return new AmpelStateService(soundService, logger, config);
    }

    [Fact]
    public void InitialState_IsStopped()
    {
        using var svc = CreateService();
        var state = svc.CurrentState;

        Assert.Equal(TimerStatus.Stopped, state.Status);
        Assert.Equal(AmpelColor.Red, state.Color);
        Assert.Equal(10, state.TimeRemaining);
        Assert.Equal("AB", state.Group);
    }

    [Fact]
    public void Start_SetsGreenAndRunning()
    {
        using var svc = CreateService();
        svc.Start();
        var state = svc.CurrentState;

        Assert.Equal(TimerStatus.Running, state.Status);
        Assert.Equal(AmpelColor.Green, state.Color);
    }

    [Fact]
    public void Pause_SetsStatusPaused()
    {
        using var svc = CreateService();
        svc.Start();
        svc.Pause();
        var state = svc.CurrentState;

        Assert.Equal(TimerStatus.Paused, state.Status);
    }

    [Fact]
    public void Stop_SetsRedAndStopped()
    {
        using var svc = CreateService();
        svc.Start();
        svc.Stop();
        var state = svc.CurrentState;

        Assert.Equal(TimerStatus.Stopped, state.Status);
        Assert.Equal(AmpelColor.Red, state.Color);
    }

    [Fact]
    public void SetGroup_UpdatesGroup()
    {
        using var svc = CreateService();
        svc.SetGroup("CD");

        Assert.Equal("CD", svc.CurrentState.Group);
    }

    [Fact]
    public void SetDuration_UpdatesTimeRemaining_WhenStopped()
    {
        using var svc = CreateService();
        svc.SetDuration(60);

        Assert.Equal(60, svc.CurrentState.TimeRemaining);
    }

    [Fact]
    public void SetColor_SetsManualOverride()
    {
        using var svc = CreateService();
        svc.SetColor(AmpelColor.Yellow);
        var state = svc.CurrentState;

        Assert.Equal(AmpelColor.Yellow, state.Color);
        Assert.True(state.ManualColorOverride);
    }

    [Fact]
    public void Start_ClearsManualOverride()
    {
        using var svc = CreateService();
        svc.SetColor(AmpelColor.Yellow);
        svc.Start();

        Assert.False(svc.CurrentState.ManualColorOverride);
        Assert.Equal(AmpelColor.Green, svc.CurrentState.Color);
    }

    [Fact]
    public void NextEnd_IncrementsEnd()
    {
        using var svc = CreateService();
        svc.NextEnd();

        Assert.Equal("2/10", svc.CurrentState.End);
    }

    [Fact]
    public void PreviousEnd_AtStart_DoesNotGoBelow1()
    {
        using var svc = CreateService();
        svc.PreviousEnd();

        Assert.Equal("1/10", svc.CurrentState.End);
    }

    [Fact]
    public void NextEnd_AtMax_DoesNotExceedTotal()
    {
        using var svc = CreateService();
        svc.SetTotalEnds(3);
        svc.NextEnd();
        svc.NextEnd();
        svc.NextEnd(); // Should not go beyond 3

        Assert.Equal("3/3", svc.CurrentState.End);
    }

    [Fact]
    public void SetTotalEnds_ClampsCurrentEnd()
    {
        using var svc = CreateService();
        // Go to end 5
        for (int i = 0; i < 4; i++) svc.NextEnd();
        Assert.Equal("5/10", svc.CurrentState.End);

        // Now reduce total to 3 — currentEnd should clamp
        svc.SetTotalEnds(3);
        Assert.Equal("3/3", svc.CurrentState.End);
    }

    [Fact]
    public void StateChanged_FiresOnStart()
    {
        using var svc = CreateService();
        AmpelState? changedState = null;
        svc.StateChanged += (_, s) => changedState = s;

        svc.Start();

        Assert.NotNull(changedState);
        Assert.Equal(TimerStatus.Running, changedState!.Status);
    }

    [Fact]
    public void Reset_RestoresDefaultTime()
    {
        using var svc = CreateService();
        svc.Start();
        Thread.Sleep(200);
        svc.Reset();
        var state = svc.CurrentState;

        Assert.Equal(TimerStatus.Stopped, state.Status);
        Assert.Equal(AmpelColor.Red, state.Color);
    }

    [Fact]
    public void Clone_ReturnsDifferentInstance()
    {
        var state = new AmpelState { TimeRemaining = 42, Group = "AB", Color = AmpelColor.Green };
        var clone = state.Clone();

        clone.TimeRemaining = 0;
        Assert.Equal(42, state.TimeRemaining);
    }

    [Fact]
    public void ToSerialJson_ProducesCorrectFormat()
    {
        var state = new AmpelState
        {
            TimeRemaining = 87,
            Group = "AB",
            Color = AmpelColor.Green,
            End = "1/10"
        };

        var json = state.ToSerialJson();
        Assert.Equal("{\"t\":87,\"g\":\"AB\",\"c\":\"G\",\"e\":\"1/10\"}", json);
    }

    [Theory]
    [InlineData(AmpelColor.Red, "R")]
    [InlineData(AmpelColor.Green, "G")]
    [InlineData(AmpelColor.Yellow, "Y")]
    public void ToSerialJson_MapsColors(AmpelColor color, string expected)
    {
        var state = new AmpelState { TimeRemaining = 0, Group = "AB", Color = color, End = "1/1" };
        Assert.Contains($"\"c\":\"{expected}\"", state.ToSerialJson());
    }

    private class FakeSoundService : ISoundService
    {
        public bool IsEnabled { get; set; } = false;
        public int Volume { get; set; } = 0;
        public void PlayTimerStart() { }
        public void PlayWarning() { }
        public void PlayTimerEnd() { }
    }
}
