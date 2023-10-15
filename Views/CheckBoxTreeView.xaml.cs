using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using SimpleBackup.Helpers;

namespace SimpleBackup.Views
{
    /// <summary>
    /// CheckBoxTreeView.xaml の相互作用ロジック
    /// </summary>
    public partial class CheckBoxTreeView : TreeView
    {
        public CheckBoxTreeView()
        {
            InitializeComponent();

            //ScrollViewerは外側にTreeViewの外側に設置
            //マウスホイールが反応するためにマウスホイールイベントを拾う
            AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler((sender, e) =>
            {
                if (!(e.Source is CheckBoxTreeView dpObj)) { return; }
                //VisualTreeを上に辿ってListBoxItemを見つける
                Dispatcher.InvokeAsync(() =>
                {
                    if (!(VisualTreeHelper.FindAncestorByType(dpObj, typeof(ScrollViewer)) is ScrollViewer psv)) { return; }
                    psv.ScrollToVerticalOffset(psv.VerticalOffset - (e.Delta / 3));
                    e.Handled = true;
                }, DispatcherPriority.Loaded);
            }));
        }
    }

    class TreeViewItemCheckBox : CheckBox
    {
        public TreeViewItemCheckBox()
        { }

        protected override void OnClick()
        {
            base.OnClick();

            if (!(DataContext is FileSystemTreeNode node)) { return; }
            node.UpdateChildStatus();
            node.UpdateParentStatus();
        }
    }
}