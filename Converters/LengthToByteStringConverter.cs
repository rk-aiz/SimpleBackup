using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleBackup.Converters
{
    [ValueConversion(typeof(long), typeof(string))]
    public class LengthToByteStringConverter : IValueConverter
    {
        public static LengthToByteStringConverter Instance { get; } = new LengthToByteStringConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is long length)) { return Binding.DoNothing; }
            if (parameter is string p)
                return LengthToByteString(length, p);
            else
                return LengthToByteString(length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        public static string LengthToByteString(long length, string format = "{0:#,0.#}")
        {
            if (length > 0)
            {
                if (length < 10)
                {
                    return String.Format($"{format} KB", 0.1);
                }
                else if (length < 1048576)
                {
                    return String.Format($"{format} KB", (float)length / (float)1024);
                }
                else if (length < 1073741824)
                {
                    return String.Format($"{format} MB", (float)length / (float)1048576);
                }
                else
                {
                    return String.Format($"{format} GB", (float)length / (float)1073741824);
                }
            }
            else if (length == 0)
            {
                return "0 KB";
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
