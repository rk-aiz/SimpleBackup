using System;
using System.Windows;
using System.Windows.Media;

namespace SimpleBackup
{
    internal class DependencyObjectSelecter
    {
        public static DependencyObject FindVisualTreeAncestorByType(DependencyObject dpobj, Type type)
        {
            try
            {
                while (dpobj != null)
                {
                    dpobj = VisualTreeHelper.GetParent(dpobj);
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
