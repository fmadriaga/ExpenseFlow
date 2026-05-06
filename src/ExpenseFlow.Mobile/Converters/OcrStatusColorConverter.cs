using System.Globalization;

namespace ExpenseFlow.Mobile.Converters;

public sealed class OcrStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string;
        return status switch
        {
            "Success" => Color.FromArgb("#16A34A"),
            "Pending" => Color.FromArgb("#D97706"),
            "Failed" => Color.FromArgb("#DC2626"),
            _ => Color.FromArgb("#6B7280"),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
