using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleBackup.Converters
{
    [ValueConversion(typeof(Boolean), typeof(Enum))]
    public class BooleanToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (!(parameter is Type enumType)) { return Binding.DoNothing; }

            if (!(value is Boolean vb)) { return Binding.DoNothing; }

            try
            {
                return Enum.Parse(enumType, (vb ? 1 : 0).ToString());
            }
            catch { return Binding.DoNothing; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
