using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AmpelSteuerung.App.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
                b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

/// <summary>
/// Converts a MatchPhase string to Visibility based on allowed states in ConverterParameter.
/// Usage: ConverterParameter="Idle|Stopped|EndCompleted" — visible when phase matches any listed state.
/// </summary>
public class StateToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string currentState && parameter is string allowedStates)
        {
            var states = allowedStates.Split('|');
            return states.Contains(currentState, StringComparer.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Multi-value converter: checks (IsFinalMode, PhaseText) to show/hide buttons per mode+state.
/// ConverterParameter format: "standard:Idle|Stopped" or "final:Idle|Stopped|EndCompleted" or "both:Idle|Stopped"
/// </summary>
public class ModeStateToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not bool isFinalMode || values[1] is not string currentPhase)
            return Visibility.Collapsed;

        if (parameter is not string param)
            return Visibility.Collapsed;

        var parts = param.Split(':');
        if (parts.Length != 2) return Visibility.Collapsed;

        var mode = parts[0].ToLowerInvariant();
        var allowedStates = parts[1].Split('|');

        var modeMatch = mode switch
        {
            "standard" => !isFinalMode,
            "final" => isFinalMode,
            "both" => true,
            _ => false
        };

        if (!modeMatch) return Visibility.Collapsed;

        return allowedStates.Contains(currentPhase, StringComparer.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
