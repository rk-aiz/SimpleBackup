using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace SimpleBackup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = FindResource("BindData") as ViewModel;

            //HwndSourceが初期化された時に呼び出される
            SourceInitialized += (sender, e) =>
            {
                //RenderModeをSoftwareOnlyに設定
                if (Properties.Settings.Default.Software_Render == true)
                {
                    HwndTarget hwndTarget = PresentationSource.FromVisual(this).CompositionTarget as HwndTarget;
                    hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                    Debug.WriteLine($"RenderMode : {hwndTarget.RenderMode}");
                }
            };

            Loaded += (sender, e) =>
            {
                //ステータスバーの更新を有効化
                StatusHelper.UpdateEnabled = true;
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Ready"));
            };
        }

        //ディレクトリ選択ダイアロクを表示
        private async void DirectoryPathTextBox_Selected(object sender, RoutedEventArgs e)
        {
            if (_vm.SchedulerEnabled == true) { return; }
            if (!(sender is DirectoryPathTextBox tb)) { return; }

            tb.Text = await CofDialogHelper.ShowDialogAsync(Dispatcher, tb.Text, tb.Description);

            var be = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            if (be?.HasError == false) { be?.UpdateSource(); }
        }

        private void BackupNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SchedulerEnabled == true)
            {
                _vm.BackupTask.BackupNow();

            }
            else
            {
                _vm.BackupTask = new BackupTask(_vm.BackupTargetDir, _vm.SaveDir, scheduled: false);
            }
        }

        private void OpenTargetDirButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWithShell(_vm.BackupTargetDir);
        }

        private void OpenSaveDirButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWithShell(_vm.SaveDir);
        }

        //何もないところをクリックしたらキーボードフォーカスをクリア
        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Grid gr)) { return; }
            if (!gr.IsMouseDirectlyOver) { return; }

            if (FocusManager.GetFocusedElement(this) is TextBox)
            {
                Keyboard.ClearFocus();
            }
        }

        //TextBoxがキーボードフォーカスを失ったときにValidationErrorがあった場合
        //バインディングソースの値に戻す
        private void TextBox_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox tb)) { return; }

            var be = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            if (be.HasError)
            {
                be.UpdateTarget();
            }
        }

        //BackupHistoryListBoxをダブルクリックしたら
        //ListBoxItemのバックアップファイルを開く
        private void BackupHistoryListBox_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is DependencyObject dpobj)) { return; }
            //VisualTreeを上に辿ってListBoxItemを見つける
            if (!(VisualTreeHelper.FindAncestorByType(dpobj, typeof(ListBoxItem)) is ListBoxItem lbm)) { return; }

            if (!(lbm.DataContext is BackupHistoryEntry entry)) { return; }

            Debug.WriteLine(entry.FileName);

            var path = System.IO.Path.Combine(entry.SaveDir, entry.FileName);
            if (System.IO.File.Exists(path))
            {
                OpenWithShell(_vm.SaveDir);
            }
        }

        private void OpenWithShell(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) { return; }

            Process.Start(path);
        }
    }
}
