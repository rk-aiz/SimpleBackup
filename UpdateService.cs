using SimpleBackup.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

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

                    //Selectorを備えたItemsControlの場合TextBlockの値を再設定
                    if ((sender is Selector selector) &&
                        (VisualTreeHelper.FindVisualTreeByType(dpObj, typeof(TextBlock)) is TextBlock tb))
                    {
                        tb.Text = selector.SelectedValue.ToStringWithTypeConverter();
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
