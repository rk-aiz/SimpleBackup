using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleBackup.Converters
{
    [ValueConversion(typeof(double), typeof(double))]
    public class LengthToGigaByteConverter : IValueConverter
    {
        public static LengthToGigaByteConverter Instance { get; } = new LengthToGigaByteConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is float length)) { return Binding.DoNothing; }

                return (float)Convert(length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strLength && float.TryParse(strLength, out float length))
            {
                return (length * 1073741824);
            }
            return Binding.DoNothing;
        }

        public static float Convert(float length)
        {
            if (length > 0)
            {
                return length / 1073741824;
            }
            else
            {
                return 0;
            }
        }
    }
}
