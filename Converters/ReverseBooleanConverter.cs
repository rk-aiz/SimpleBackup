using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleBackup.Converters
{
    /// <summary>
    /// Booleanを反転する
    /// </summary>
    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class ReverseBooleanConverter : IValueConverter
    {
        public static ReverseBooleanConverter Instance = new ReverseBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool v && v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool v && v);
        }
    }
}
