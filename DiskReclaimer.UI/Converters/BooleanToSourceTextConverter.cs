using System.Globalization;
using System.Windows.Data;

namespace DiskReclaimer.UI.Converters;

/// <summary>Renders ExclusionRule.IsSystemFloor as a human label for the grid's "Source" column.</summary>
public sealed class BooleanToSourceTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Built-in" : "User";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
