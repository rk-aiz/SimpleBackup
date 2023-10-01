using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SimpleBackup
{
    /// <summary>
    /// 定期的にバックアップを行うクラス
    /// </summary>
    internal class BackupTask
    {
        //バックアップ対象フォルダ
        private readonly string _target;

        //バックアップ保存先フォルダ
        private readonly string _save;

        //定期バックアップのインターバル
        private readonly uint _timerInterval = 10;

        //定期バックアップ用タイマー
        private DispatcherTimer _timer;

        //バックアップ処理中のロック
        private readonly Object _lockObject = new Object();
        private CancellationTokenSource cTokenSource;

        //バックアップ終了時のイベント
        public event BackupCompletedEventHandler BackupCompleted;

        public BackupTask(string target, string save, uint timerInterval = 10, bool scheduled = true)
        {
            _target = target;
            _save = save;
            _timerInterval = timerInterval;
            if (scheduled)
            {
                SetupTimer();
            }
        }

        public void BackupNow()
        {
            BackupMethod(this, EventArgs.Empty);
        }

        private async void BackupMethod(object sender, EventArgs e)
        {
            //バックアップ処理が既に実行中の場合中止
            if (cTokenSource != null) { return; }

            //バックアップ対象が存在しない場合中止
            if (String.IsNullOrWhiteSpace(_target) || Directory.Exists(_target) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_target_does_not_exist"));
                return;
            }

            //バックアップ保存場所が存在しない場合中止
            DirectoryInfo diSave = new DirectoryInfo(_save);
            if (String.IsNullOrWhiteSpace(_save) || Directory.Exists(_save) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Save_Location_does_not_exist"));
                return;
            }

            var saveName = $"{Path.GetFileName(_target)}-{DateTime.Now:yyyyMMdd-hh-mm-ss}.zip";
            var savePath = Path.Combine(_save, saveName);

            //バックアップファイルが既に存在する場合中止
            if (File.Exists(savePath)) { return; }

            DirectoryInfo diTarget = new DirectoryInfo(_target);
            await CreateZipAsync(diTarget, savePath);

            if (System.IO.File.Exists(savePath) == true)
            {
                OnBackupCompleted(_target, _save, saveName);

                var msg = $"{LocalizeHelper.GetString("String_Backup_process_completed")} -> {savePath}";
                StatusHelper.UpdateStatus(msg);
                Debug.WriteLine($"Backup -> {savePath}");
            }

        }

        private void OnBackupCompleted(string src, string dir, string name)
        {
            BackupCompleted?.Invoke(this, new BackupCompletedEventArgs(src, dir, name));
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMinutes(_timerInterval)
            };
            _timer.Tick += new EventHandler(BackupMethod);
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer?.Stop();
        }

        private Task CreateZipAsync(DirectoryInfo src, string path)
        {
            StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_process_in_progress"));
            StatusHelper.SetProgressStatus(true);

            cTokenSource = new CancellationTokenSource();
            return Task.Run(() => CreateZip(src, path)).ContinueWith(t =>
            {
                cTokenSource?.Dispose();
                cTokenSource = null;
                StatusHelper.SetProgressStatus(false);
            });
        }

        private void CreateZip(DirectoryInfo src, string path)
        {
            lock (_lockObject)
            {
                src.CreateZipArchive(path);
            }
        }

        ~BackupTask()
        {
            lock (_lockObject)
            {
                _timer?.Stop();
            }
        }
    }
}
