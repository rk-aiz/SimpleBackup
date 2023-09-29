using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SimpleBackup
{
    internal class BackupTask : IDisposable
    {
        private readonly string _target;
        private readonly string _save;
        private readonly uint _timerInterval = 10;
        private DispatcherTimer _timer;

        private readonly Object _lockObject = new Object();
        private CancellationTokenSource cTokenSource = null;

        public delegate void BackupCompletedEventHandler(object sender, BackupCompletedEventArgs e);
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
            Debug.WriteLine("BackupTask BackupNow called");
            BackupMethod(this, EventArgs.Empty);
        }

        private Task CreateZipAsync(DirectoryInfo src, string path)
        {
            var msg = LocalizeHelper.GetString("String_Backup_process_in_progress");
            StatusHelper.UpdateStatus(msg);
            StatusHelper.ShowProgressRing();

            cTokenSource = new CancellationTokenSource();
            return Task.Run(() => CreateZip(src, path)).ContinueWith(t =>
            {
                cTokenSource?.Dispose();
                cTokenSource = null;
                StatusHelper.HideProgressRing();
            });
        }

        private void CreateZip(DirectoryInfo src, string path)
        {
            lock (_lockObject)
            {
                src.CreateZipArchive(path);
            }
        }

        public async void BackupMethod(object sender, EventArgs e)
        {
            //バックアップ処理が既に実行中の場合中止
            if (cTokenSource != null)
            {
                return;
            }

            //バックアップ対象が存在しない場合中止
            DirectoryInfo diTarget = new DirectoryInfo(_target);
            if (diTarget.Exists == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_target_does_not_exist"));
                return;
            }

            //バックアップ保存場所が存在しない場合中止
            DirectoryInfo diSave = new DirectoryInfo(_save);
            if (diSave.Exists == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Save_Location_does_not_exist"));
                return;
            }

            var saveName = $"{Path.GetFileName(_target)}-{DateTime.Now:yyyyMMdd-hh-mm-ss}.zip";
            var savePath = Path.Combine(_save, saveName);

            //バックアップファイルが既に存在する場合中止
            if (System.IO.File.Exists(savePath))
            {
                return;
            }

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

        private void StopTimer(object sender, CancelEventArgs e)
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                Debug.WriteLine("BackupTask destructor called");
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer = null;
                }
            }
        }
    }
}
