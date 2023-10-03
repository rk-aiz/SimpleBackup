using System.ComponentModel;
using System.Resources;

namespace SimpleBackup
{
    public static class LocalizeHelper
    {
        private static ResourceManager _rm = new ResourceManager(typeof(SimpleBackup.Properties.Resources));

        public static string GetString(string key)
        {
            return _rm.GetString(key);
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ToggleStateMessage
    {
        [LocalizedDescription("String_Off",
        typeof(SimpleBackup.Properties.Resources))]
        Off,

        [LocalizedDescription("String_On",
        typeof(SimpleBackup.Properties.Resources))]
        On,
    }
}
