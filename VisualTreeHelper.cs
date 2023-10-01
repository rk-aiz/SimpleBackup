using System;
using System.Windows;

namespace SimpleBackup
{
    /// <summary>
    /// VisualTreeHelperの拡張
    /// </summary>
    internal class VisualTreeHelper
    {
        public static DependencyObject FindAncestorByType(DependencyObject dpobj, Type type)
        {
            try
            {
                while (dpobj != null)
                {
                    dpobj = System.Windows.Media.VisualTreeHelper.GetParent(dpobj);
                    if (type == dpobj?.DependencyObjectType.SystemType)
                    {
                        return dpobj;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
