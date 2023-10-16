using SimpleBackup.Converters;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace SimpleBackup
{
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
    /// <summary>
    /// Enum型をItemsSourceとして利用するためのBinding <- TargetUpdateでLocalizedDescriptionが更新されるのでこっち使う
    /// (例：EnumをComboBoxの選択肢として利用する)
    /// </summary>
    public class EnumTypeSourceBinding : Binding
    {
        private readonly Type _enumType;

        public object EnumValues
        {
            get { return Enum.GetValues(_enumType); }
        }

        public EnumTypeSourceBinding(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"{enumType} is not enum.");
            }
            _enumType = enumType;
            Path = new PropertyPath("EnumValues");
            Source = this;
            Mode = BindingMode.OneWay;
        }
    }

    /// <summary>
    /// Enum型をItemsSourceとして利用するためのマークアップ拡張
    /// (例：EnumをComboBoxの選択肢として利用する)
    /// </summary>
    public class EnumTypeSource : MarkupExtension
    {
        private readonly Type _enumType;

        public EnumTypeSource(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"{enumType} is not enum.");
            }

            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(_enumType);
    }
}