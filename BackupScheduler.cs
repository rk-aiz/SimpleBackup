using System;
using System.Windows.Threading;

namespace SimpleBackup
{
    /// <summary>
    /// 定期的にバックアップを行うクラス
    /// </summary>
    internal class BackupScheduler
    {
        //バックアップ対象フォルダ
        private readonly string _target;

        //バックアップ保存先フォルダ
        private readonly string _save;

        //定期バックアップのインターバル
        private readonly uint _timerInterval = 10;

        //定期バックアップ用タイマー
        private DispatcherTimer _timer;

        public BackupScheduler(string target, string save, uint timerInterval = 10, bool scheduled = true)
        {
            _target = target;
            _save = save;
            _timerInterval = timerInterval;
            if (scheduled)
            {
                SetupTimer();
            }
        }

        /*public void BackupNow()
        {
            BackupMethod(this, EventArgs.Empty);
        }*/

        private void SetupTimer()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMinutes(_timerInterval)
            };
            _timer.Tick += (s, e) => { ViewModel.Instance.CreateBackupTask(_target, _save); };
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer?.Stop();
        }

        /*

        ~BackupScheduler()
        {
            lock (_lockObject)
            {
                _timer?.Stop();
            }
        }*/
    }
}
