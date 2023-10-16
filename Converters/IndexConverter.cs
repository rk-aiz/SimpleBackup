using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleBackup.Converters
{
    [ValueConversion(typeof(Int32), typeof(String))]
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Int32 res)
                return $"{res + 1}:";
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
