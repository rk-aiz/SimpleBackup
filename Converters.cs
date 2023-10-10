using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;


namespace SimpleBackup
{
    public class CultureAwareBinding : Binding
    {
        public CultureAwareBinding()
        {
            ConverterCulture = CultureInfo.CurrentUICulture;
        }
    }

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

    public class EnumDescriptionTypeConverter : EnumConverter
    {
        public EnumDescriptionTypeConverter(Type type) :
            base(type)
        { }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string) || value == null)
                return base.ConvertTo(context, culture, value, destinationType);

            FieldInfo fi = value.GetType().GetField(value.ToString());
            if (fi == null)
                return base.ConvertTo(context, culture, value, destinationType);

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            return (attributes.Length > 0 && !string.IsNullOrEmpty(attributes[0].Description)) ?
                attributes[0].Description : value.ToString();
        }
    }

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

    /// <summary>
    /// Booleanを反転する
    /// </summary>
    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class ReverseBooleanConverter : IValueConverter
    {
        public static ReverseBooleanConverter Instance = new ReverseBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }
    }

    [ValueConversion(typeof(long), typeof(String))]
    public class LengthToByteStringConverter : IValueConverter
    {
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
            if (value == null)
                return Binding.DoNothing;

            long backValue;
            if (long.TryParse((String)value, out backValue))
                return backValue;

            return Binding.DoNothing;
        }

        public string LengthToByteString(long length, string format = "{0:#,0.0}")
        {
            if (length > 0)
            {
                if (length < 1024)
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
            else
            {
                return "0 KB";
            }
        }
    }
}
