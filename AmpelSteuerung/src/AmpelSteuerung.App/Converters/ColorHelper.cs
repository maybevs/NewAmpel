using System.Windows.Media;

namespace AmpelSteuerung.App.Converters;

public static class ColorHelper
{
    public static Brush ToBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}
