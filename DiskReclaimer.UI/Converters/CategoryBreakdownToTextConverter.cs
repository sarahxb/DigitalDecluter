using System.Globalization;
using System.Windows.Data;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.UI.Converters;

public sealed class CategoryBreakdownToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IReadOnlyDictionary<Category, long> breakdown
            ? string.Join("; ", breakdown.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"))
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
