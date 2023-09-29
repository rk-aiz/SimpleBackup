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
}
