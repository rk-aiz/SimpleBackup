﻿using System;
using System.Diagnostics;
using System.Windows;

namespace SimpleBackup.Helpers
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

        public static DependencyObject FindVisualTree(DependencyObject dpObj, string findName)
        {
            try
            {
                //Debug.WriteLine(dpObj);
                if (dpObj.GetValue(FrameworkElement.NameProperty) is string name)
                    if (name == findName)
                    {
                        return dpObj;
                    }

                int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(dpObj);
                if (count > 0)
                {
                    // VisualTreeを再帰探査
                    for (int i = 0; i < count; i++)
                    {

                        DependencyObject result = FindVisualTree(System.Windows.Media.VisualTreeHelper.GetChild(dpObj, i), findName);

                        if (result != null)
                        {
                            return result;
                        }
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        public static DependencyObject FindVisualTreeByType(DependencyObject dpObj, Type type)
        {
            try
            {
                //Debug.WriteLine(dpObj);
                if (dpObj.GetType() == type)
                {
                    return dpObj;
                }

                int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(dpObj);
                if (count > 0)
                {
                    // VisualTreeを再帰探査
                    for (int i = 0; i < count; i++)
                    {

                        DependencyObject result = FindVisualTreeByType(System.Windows.Media.VisualTreeHelper.GetChild(dpObj, i), type);

                        if (result != null)
                        {
                            return result;
                        }
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
