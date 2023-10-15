using SimpleBackup.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleBackup
{
    public class CultureAwareBinding : Binding
    {
        public string Key
        {
            set
            {
                Path = new PropertyPath("Resources." + value);
                Source = LocalizeHelper.Instance;
            }
        }

        public CultureAwareBinding()
        {
            ConverterCulture = CultureInfo.CurrentUICulture;
        }
    }
}
