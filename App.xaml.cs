using SimpleBackup.Properties;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
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
                try
                {
                    Settings.Default.Upgrade();
                }
                catch { }
                Settings.Default.IsFirstRun = false;

                //Delete the old user.config file
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
                try
                {
                    var di = Directory.GetParent(config.FilePath).Parent;
                    var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                    if (di.Name.Contains(assemblyName))
                    {
                        DirectoryCleaning.DeleteAllExcept(di.FullName, config.FilePath);
                    }
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

        /// <summary>
        /// 常駐開始時の初期化処理
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            //継承元のOnStartupを呼び出す
            base.OnStartup(e);

            using (var appContext = new System.Windows.Forms.ApplicationContext())
            {

                if (TaskTray.Instance.TaskTrayMode != TaskTrayMode.TrayIconOnly)
                {
                    TaskTray.Instance.ShowWindow();
                }

                System.Windows.Forms.Application.Run(appContext);
            }
            Debug.WriteLine("Shutdown");
            Shutdown();
        }
    }
}
