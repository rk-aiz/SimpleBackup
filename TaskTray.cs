using SimpleBackup.Properties;
using SimpleBackup.Views;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace SimpleBackup
{
    /// <summary>
    /// タスクトレイアイコンForm
    /// </summary>
    internal class TaskTray : Form, INotifyPropertyChanged
    {
        public static TaskTray Instance { get; } = new TaskTray();

        private NotifyIcon _notifyIcon;
        public NotifyIcon NotifyIcon
        {
            get { return _notifyIcon; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// タスクトレイ常駐モード
        /// </summary>
        public TaskTrayMode TaskTrayMode
        {
            get { return (TaskTrayMode)Settings.Default.TaskTrayMode; }
            set
            {
                Settings.Default.TaskTrayMode = (int)value;
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = Convert.ToBoolean(value);
                }
                NotifyPropertyChanged();
            }
        }

        // MainWindow２重起動防止用
        private MainWindow _mainWindow; //ここでnewしないこと(バインディングがうまくいかない)
        private TaskTray()
        {
            var iconResourceInfo = System.Windows.Application.GetResourceStream(new Uri("icon.ico", UriKind.Relative)).Stream;

            //通知領域にアイコンを表示
            _notifyIcon = new NotifyIcon
            {
                Visible = Convert.ToBoolean(TaskTrayMode),
                Icon = new Icon(iconResourceInfo),
                Text = "Simple Backup"
            };

            _notifyIcon.MouseDoubleClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                {
                    ShowWindow();
                }
            };

            _notifyIcon.ContextMenuStrip = InitializeMenuStrip();
        }

        public void AddToolStripItem(ToolStripItem item)
        {
            this._notifyIcon.ContextMenuStrip.Items.Insert(0, item);
        }

        /// <summary>
        /// コンテキストメニューの初期化
        /// </summary>
        private ContextMenuStrip InitializeMenuStrip()
        {
            var menuStrip = new ContextMenuStrip();

            menuStrip.Items.Add("Exit", null, (s, e) => Close());

            return menuStrip;
        }

        /// <summary>
        /// 設定画面を表示
        /// </summary>
        public void ShowWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow
                {
                    //ウィンドウを画面中央に表示
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                //キー入力のForm => WPF 転送
                ElementHost.EnableModelessKeyboardInterop(_mainWindow);
            }
            _mainWindow.Show();
        }

        /// <summary>
        /// 終了処理
        /// FormのClose()を隠すためnew
        /// </summary>
        new public void Close()
        {
            if (null != _notifyIcon)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.Close();
            System.Windows.Forms.Application.ExitThread();
        }
    }
}
