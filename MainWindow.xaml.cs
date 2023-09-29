using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
            _vm = FindResource("bindData") as ViewModel;

            if (FindResource("WaitStoryboard") is Storyboard sb)
            {
                StatusHelper.ProgressAnimation = sb;
            }

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
                StatusHelper.UpdateEnabled = true;
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Ready"));
            };

            Closing += (sender, e) =>
            {
                _vm.Dispose();
            };
        }

        private async void TargetDir_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vm.ScheduleEnabled == true)
                return;

            _vm.BackupTargetDir = await ShowCofDialogAsync(
                _vm.BackupTargetDir,
                LocalizeHelper.GetString("String_Select_Backup_Folder")
            );
        }

        private async void SaveDir_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vm.ScheduleEnabled == true)
                return;

            _vm.SaveDir = await ShowCofDialogAsync(
                _vm.SaveDir,
                LocalizeHelper.GetString("String_Select_Save_Location")
            );
        }

        private DispatcherOperation<string> ShowCofDialogAsync(string initPath, string title = "Select Folder")
        {
            return Dispatcher.InvokeAsync<string>(() => ShowCofDialog(initPath, title));
        }

        private string ShowCofDialog(string initPath, string title)
        {
            //Debug.WriteLine(path);
            using (var cofDialog = new CommonOpenFileDialog()
            {
                Title = title,
                IsFolderPicker = true,
                InitialDirectory = initPath
            })
            {
                if (cofDialog.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return initPath;
                }
                else
                {
                    return cofDialog.FileName;
                }
            }
        }

        private void BackupNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ScheduleEnabled == true)
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
            System.Diagnostics.Process.Start("explorer.exe", _vm.BackupTargetDir);
        }

        private void OpenSaveDirButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", _vm.SaveDir);
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("RootGrid_MouseDown");
            if (sender is Grid gr)
            {
                if (gr.IsMouseDirectlyOver)
                {
                    IInputElement focusedElement = FocusManager.GetFocusedElement(this);
                    Debug.WriteLine("Type: " + focusedElement.GetType().ToString());
                    if (focusedElement is TextBox)
                    {
                        //BreakFocus(focusedElement as Control);
                        System.Windows.Input.Keyboard.ClearFocus();
                    }
                }
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TextBox_LostFocus");
            if (sender is TextBox tb)
            {
                var bindingExpression = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                if (bindingExpression.HasError)
                {
                    bindingExpression.UpdateTarget();
                }
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ListBoxItem_MouseDoubleClick");
            if (e.OriginalSource is DependencyObject dpobj)
            {
                ListBoxItem lbm = DependencyObjectSelecter.FindVisualTreeAncestorByType(dpobj, typeof(ListBoxItem)) as ListBoxItem;

                if (lbm?.DataContext is BackupHistoryEntry entry)
                {
                    Debug.WriteLine(entry.FileName);

                    var path = System.IO.Path.Combine(entry.SaveDir, entry.FileName);
                    if (System.IO.File.Exists(path))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                }
            }

        }

        /*
        private void BreakFocus(Control control)
        {
            DependencyObject ancestor = control.Parent;
            while (ancestor != null)
            {
                // フォーカスできるか
                if (ancestor is UIElement element && element.Focusable)
                {
                    element.Focus(); // フォーカスを当てる
                    break;
                }
                ancestor = VisualTreeHelper.GetParent(ancestor);
            }
        }
        */
    }
}
