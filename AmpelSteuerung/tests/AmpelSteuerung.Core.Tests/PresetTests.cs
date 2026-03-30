using AmpelSteuerung.Core.Models;

namespace AmpelSteuerung.Core.Tests;

public class PresetTests
{
    [Fact]
    public void Preset_Defaults()
    {
        var preset = new Preset();
        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal(10, preset.Ends);
        Assert.Equal(3, preset.ArrowsPerEnd);
        Assert.Equal(120, preset.TimerDuration);
        Assert.Equal(30, preset.WarningTime);
    }

    [Fact]
    public void PresetAction_JsonProperties()
    {
        var action = new PresetAction
        {
            Action = "setGroup",
            Value = "AB",
            Duration = null
        };

        Assert.Equal("setGroup", action.Action);
        Assert.Equal("AB", action.Value);
        Assert.Null(action.Duration);
    }
}
