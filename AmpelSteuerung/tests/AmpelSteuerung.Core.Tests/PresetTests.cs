using AmpelSteuerung.Core.Models;

namespace AmpelSteuerung.Core.Tests;

public class PresetTests
{
    [Fact]
    public void Preset_Defaults()
    {
        var preset = new Preset();
        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal("standard", preset.Type);
        Assert.False(preset.IsFinalMode);
        Assert.Equal(120, preset.Timer.ShootingTime);
        Assert.Equal(10, preset.Timer.PreparationTime);
        Assert.Equal(30, preset.Timer.WarningTime);
        Assert.Equal(10, preset.Match.TotalEnds);
        Assert.Equal(3, preset.Match.ArrowsPerEnd);
    }

    [Fact]
    public void Preset_IsFinalMode_WhenTypeFinal()
    {
        var preset = new Preset { Type = "final" };
        Assert.True(preset.IsFinalMode);
    }

    [Fact]
    public void PresetFinalSettings_Defaults()
    {
        var final = new PresetFinalSettings();
        Assert.Equal(1, final.ArrowsPerSide);
        Assert.Equal(3, final.TotalArrowsPerEnd);
        Assert.Equal(2, final.Sides.Length);
    }

    [Fact]
    public void PresetGroupSettings_Defaults()
    {
        var groups = new PresetGroupSettings();
        Assert.Equal("alternating", groups.Mode);
        Assert.Equal(2, groups.Names.Length);
        Assert.True(groups.AlternateStartOrder);
    }
}
