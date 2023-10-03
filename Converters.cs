using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
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
}
