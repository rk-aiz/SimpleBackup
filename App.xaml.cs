using System;
using System.Globalization;
using System.Windows;

namespace SimpleBackup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LoadLocale();

            Exit += (sender, e) =>
            {
                SimpleBackup.Properties.Settings.Default.Save();
            };
        }

        private void LoadLocale()
        {
            string locale;
            try
            {
                locale = SimpleBackup.Properties.Settings.Default.Locale;
            }
            catch (Exception)
            {
                return;
            }

            if (string.Compare(locale, "Default", true) != 0)
            {
                try
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale, false);
                }
                catch { }
            }
        }

    }
}
