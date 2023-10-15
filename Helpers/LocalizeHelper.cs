using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SimpleBackup.Properties;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel.Design;

namespace SimpleBackup.Helpers
{
    public class LocalizeHelper : INotifyPropertyChanged, INotifyUpdate
    {
        public static LocalizeHelper Instance { get; } = new LocalizeHelper();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler NotifyUpdate;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ResourceManager _rm = new ResourceManager(typeof(SimpleBackup.Properties.Resources));

        private readonly Resources _resources = new Resources();
        public Resources Resources
        {
            get { return this._resources; }
        }

        public ObservableCollection<CultureInfo> CultureCollection { get; } = new ObservableCollection<CultureInfo>();

        private CultureInfo _uiCulture;
        public CultureInfo UICulture
        {
            get { return _uiCulture; }
            set
            {
                _uiCulture = value;
                try
                {
                    CultureInfo.CurrentUICulture = value;
                    Settings.Default.Locale = value.Name;
                    ChangeCulture(value);
                }
                catch { }
                NotifyPropertyChanged();
            }
        }

        private LocalizeHelper()
        {
            FindLocalizedResourceFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        /// <summary>
        /// 対応しているリソースファイルの言語リストを取得する
        /// </summary>
        public void FindLocalizedResourceFiles(string location)
        {
            var cts = new CancellationTokenSource(3000);
            try
            {
                FindLocalizedResourceFilesCore(location, cts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private Task FindLocalizedResourceFilesCore(string location, CancellationTokenSource cts)
        {
            return Task.Run(() =>
            {
                Resources.Culture = CultureInfo.CurrentUICulture;
                CultureCollection.Add(new CultureInfo("en-US", false));

                var dirInfo = new DirectoryInfo(location);
                foreach (DirectoryInfo di in dirInfo.GetDirectories())
                {
                    try
                    {
                        var ci = new CultureInfo(di.Name, false);
                        if (ci == null) { continue; }
                        CultureCollection.Add(ci);
                    }
                    catch { }
                }
                foreach (CultureInfo ci in CultureCollection)
                {
                    if (ci.Equals(CultureInfo.CurrentUICulture))
                    {
                        UICulture = ci;
                    }
                }
            }, cts.Token).ContinueWith(t => cts.Dispose());
        }

        public static string GetString(string key)
        {
            return Instance?._rm.GetString(key) ?? key;
        }

        public static string GetEnumLocalizeDescription(object value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            if (fi == null) { return value.ToString(); }

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            return (attributes.Length > 0 && !string.IsNullOrEmpty(attributes[0].Description)) ?
                attributes[0].Description : value.ToString();
        }

        /// <summary>
        /// リソースのカルチャを変更する
        /// </summary>
        public void ChangeCulture(CultureInfo newCulture)
        {
            var oldCultureName = Resources.Culture.Name;
            Resources.Culture = newCulture;
            NotifyPropertyChanged("Resources");

            NotifyUpdate?.Invoke(this, new EventArgs());

            var oldCulture = new CultureInfo(oldCultureName);
            StatusHelper.UpdateStatus($"{GetString("String_Language")} : {oldCulture.DisplayName} -> {newCulture.DisplayName}");
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

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Priority
    {
        [LocalizedDescription("String_Lowest",
            typeof(SimpleBackup.Properties.Resources))]
            Lowest,
        [LocalizedDescription("String_Below_Normal",
            typeof(SimpleBackup.Properties.Resources))]
            BelowNormal,
        [LocalizedDescription("String_Normal",
            typeof(SimpleBackup.Properties.Resources))]
            Normal,
        [LocalizedDescription("String_Above_Normal",
            typeof(SimpleBackup.Properties.Resources))]
            AboveNormal,
        [LocalizedDescription("String_Highest",
            typeof(SimpleBackup.Properties.Resources))]
            Highest
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum TaskTrayMode
    {
        [LocalizedDescription("String_Show_Window_Only",
            typeof(SimpleBackup.Properties.Resources))]
        ShowWindowOnly,
        [LocalizedDescription("String_Show_Window_And_TrayIcon",
            typeof(SimpleBackup.Properties.Resources))]
        ShowWindowAndTrayIcon,
        [LocalizedDescription("String_Tray_Icon_Only",
            typeof(SimpleBackup.Properties.Resources))]
        TrayIconOnly
    }
}
