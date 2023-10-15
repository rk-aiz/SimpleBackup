using System;
using System.Windows;
using System.Windows.Markup;
using System.Linq;
using SimpleBackup.Helpers;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Data;

namespace SimpleBackup
{
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

    /// <summary>
    /// Enum型をItemsSourceとして利用するためのBinding <- TargetUpdateでLocalizedDescriptionが更新されるのでこっち使う
    /// (例：EnumをComboBoxの選択肢として利用する)
    /// </summary>
    public class EnumTypeSourceBinding : Binding
    {
        private readonly Type _enumType;

        public object EnumValues
        {
            get
            {
                if (_enumType != null)
                {
                    return Enum.GetValues(_enumType);
                }
                else
                    return null;
            }
        }

        public EnumTypeSourceBinding(Type enumType)
        {
            _enumType = enumType;
            Path = new PropertyPath("EnumValues");
            Source = this;
            Mode = BindingMode.OneWay;
        }
    }
}