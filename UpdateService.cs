﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using System.Diagnostics;
using SimpleBackup.Extensions;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace SimpleBackup.Helpers
{
    public interface INotifyUpdate
    {
        event EventHandler NotifyUpdate;
    }

    /// <summary>
    /// NotifyUpdateイベントをトリガーとしてBindingExpression.UpdateTarget()
    /// を実行する為の添付プロパティ
    /// プロパティの値にINotifyUpdateインターフェースを備えたクラスのインスタンスを設定する
    /// </summary>
    public class UpdateService : DependencyObject
    {
        public static readonly DependencyProperty TextUpdaterProperty =
        DependencyProperty.RegisterAttached(
            "TextUpdater",
            typeof(INotifyUpdate),
            typeof(UpdateService),
            new PropertyMetadata(null, (sender, e) =>
            {
                if (!(e.NewValue is INotifyUpdate notifier)) { return; }
                notifier.NotifyUpdate += (n, args) =>
                {
                    if (!(sender is DependencyObject dpObj)) { return; }
                    var expression = BindingOperations.GetBindingExpression(dpObj, TextBlock.TextProperty);
                    expression.UpdateTarget();
                };
            })
        );
        public static void SetTextUpdater(UIElement element, INotifyUpdate value)
        {
            element.SetValue(TextUpdaterProperty, value);
        }
        public static INotifyUpdate GetTextUpdater(UIElement element)
        {
            return (INotifyUpdate)element.GetValue(TextUpdaterProperty);
        }

        public static readonly DependencyProperty ItemsSourceUpdaterProperty =
        DependencyProperty.RegisterAttached(
            "ItemsSourceUpdater",
            typeof(INotifyUpdate),
            typeof(UpdateService),
            new PropertyMetadata(null, (sender, e) =>
            {
                if (!(e.NewValue is INotifyUpdate notifier)) { return; }
                notifier.NotifyUpdate += (n, args) =>
                {
                    if (!(sender is DependencyObject dpObj)) { return; }
                    BindingOperations.GetBindingExpression(dpObj, ItemsControl.ItemsSourceProperty)?.UpdateTarget();

                    if ((sender is Selector selector) &&
                        (VisualTreeHelper.FindVisualTreeByType(dpObj, typeof(TextBlock)) is TextBlock tb))
                    {
                        tb.Text = LocalizeHelper.GetEnumLocalizeDescription(selector.SelectedValue);
                    }
                };
            })
        );

        public static void SetItemsSourceUpdater(UIElement element, INotifyUpdate value)
        {
            element.SetValue(ItemsSourceUpdaterProperty, value);
        }
        public static INotifyUpdate GetItemsSourceUpdater(UIElement element)
        {
            return (INotifyUpdate)element.GetValue(ItemsSourceUpdaterProperty);
        }
    }
}