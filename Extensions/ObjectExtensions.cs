using System.ComponentModel;

namespace SimpleBackup.Extensions
{
    internal static class ObjectExtensions
    {
        public static string ToStringWithTypeConverter(this object value) => TypeDescriptor.GetConverter(value.GetType()).ConvertToString(value);
    }
}
