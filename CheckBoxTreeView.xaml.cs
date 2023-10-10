﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
namespace SimpleBackup
{
    /// <summary>
    /// CheckBoxTreeView.xaml の相互作用ロジック
    /// </summary>
    public partial class CheckBoxTreeView : TreeView
    {
        public CheckBoxTreeView()
        {
            InitializeComponent();
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((sender, e) =>
            {
                if (!(e.OriginalSource is CheckBox cb)) { return; }
                if (!(cb.DataContext is FileSystemTreeNode node)) { return; }

                node.UpdateChildStatus();
                node.UpdateParentStatus();
                Debug.WriteLine(node.GetPath());
            }));
        }
    }
}