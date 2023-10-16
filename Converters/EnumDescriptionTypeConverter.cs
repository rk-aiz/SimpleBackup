using SimpleBackup.Extensions;
using System;
using System.ComponentModel;
using System.Globalization;

namespace SimpleBackup.Converters
{
    public class EnumDescriptionTypeConverter : EnumConverter
    {
        public EnumDescriptionTypeConverter(Type type) : base(type)
        { }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) &&
                value is Enum enumValue &&
                enumValue.GetAttribute<DescriptionAttribute>() is DescriptionAttribute attr)
            {
                return attr.Description;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
