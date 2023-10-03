using System;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Diagnostics;
using SimpleBackup.Properties;
using System.Configuration;
using System.Collections;
using System.Runtime.Remoting.Contexts;

namespace SimpleBackup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            CheckFirstRun();
            LoadLocale();

            Exit += (sender, e) =>
            {
                Settings.Default.Save();
            };
        }

        private void CheckFirstRun()
        {
            if (Settings.Default.IsFirstRun)
            {
                Debug.WriteLine("Settings Upgrade");
                Settings.Default.Upgrade();
                Settings.Default.IsFirstRun = false;

                //Delete the old user.config file
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
                try
                {
                    Directory.GetParent(config.FilePath).Parent.Delete(true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                Settings.Default.Save();
            }
        }

        private void LoadLocale()
        {
            try
            {
                string locale = Settings.Default.Locale;
                if (string.Compare(locale, "Default", true) != 0)
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale, false);
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
