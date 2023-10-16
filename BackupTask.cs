using Newtonsoft.Json;
using SimpleBackup.Commands;
using SimpleBackup.Events;
using SimpleBackup.Helpers;
using SimpleBackup.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace SimpleBackup
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    internal enum BackupTaskStatus
    {
        [LocalizedDescription("String_Processing", typeof(Properties.Resources))]
        Processing,
        [LocalizedDescription("String_Completed", typeof(Properties.Resources))]
        Completed,
        [LocalizedDescription("String_Failed", typeof(Properties.Resources))]
        Failed,
    }

    /// <summary>
    /// 1回分のバックアップ処理を扱うクラス
    /// </summary>
    internal class BackupTask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// バックアップ元・バックアップ保存先が同じ場合、一連のバックアップシークエンスとして扱うためのフラグ
        /// </summary>
        private bool _InSequence = true;
        [JsonIgnore]
        public bool InSequence
        {
            get { return _InSequence; }
            set { _InSequence = value; OnPropertyChanged("InSequence"); }
        }

        public string SourcePath { get; set; }

        private string _saveDir;
        public string SaveDir
        {
            get { return _saveDir; }
            set { _saveDir = value; OnPropertyChanged("SaveDir"); }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; OnPropertyChanged("FileName"); }
        }

        private DateTime _saveTime;
        public DateTime SaveTime
        {
            get { return _saveTime; }
            set { _saveTime = value; OnPropertyChanged("SaveTime"); }
        }

        private long _fileSize = -1;
        public long FileSize
        {
            get { return _fileSize; }
            set { _fileSize = value; OnPropertyChanged("FileSize"); }
        }

        private int _index;
        public int Index
        {
            get { return _index; }
            set { _index = value; OnPropertyChanged("Index"); }
        }

        private BackupTaskStatus _status;
        public BackupTaskStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
                //((DelegateCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }

        private int _progress;
        [JsonIgnore]
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (_progress < value)
                {
                    _progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        [JsonIgnore]
        public List<FileSystemInfo> TargetItems { get; set; }

        //バックアップ処理中のロック
        private CancellationTokenSource cTokenSource;

        /// <summary>
        /// バックアップ処理完了時のイベント
        /// </summary>
        public event BackupCompletedEventHandler BackupCompleted;

        private readonly ThreadPriority _priority;
        private string _savePath;

        public BackupTask(ThreadPriority priority)
        {
            _priority = priority;
            _saveTime = DateTime.Now;
            _status = BackupTaskStatus.Processing;
        }

        /// <summary>
        /// バックアップ処理実行
        /// </summary>
        public void Backup()
        {
            //バックアップ処理が既に実行中の場合中止
            if (cTokenSource != null) { return; }

            //ステータスバー更新
            StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_process_in_progress"));
            StatusHelper.SetProgressStatus(true);

            //バックアップ処理中は設定変更できないようにする
            StatusHelper.RequestLockSetting();

            //Destination Path
            _savePath = System.IO.Path.Combine(SaveDir, FileName);
            DirectoryInfo diTarget = new DirectoryInfo(SourcePath);
            cTokenSource = new CancellationTokenSource();

            var zah = new Helpers.ZipArchiveHelper(diTarget, TargetItems, _savePath, cTokenSource.Token);

            var dp = Dispatcher.CurrentDispatcher;
            zah.ProgressChanged += (s, e) => dp.InvokeAsync(() => ProgressChangedCallback(s, e));

            zah.ContinueWith = (ex) => ThreadContinueMethod(ex);

            ThreadStart ts = new ThreadStart(() => zah.CreateZipArchive());

            ts += () => ThreadCompletedCallback();

            Thread thread = new Thread(ts)
            {
                IsBackground = true,
                Priority = _priority,
            };
            thread.Start();
        }

        /// <summary>
        /// スレッド完了後の処理
        /// キャンセルかエラーがあった場合バックアップ失敗として処理
        /// </summary>
        /// <param name="ex"></param>
        private void ThreadContinueMethod(Exception ex)
        {
            if (ex == null)
            {
                FileInfo fi = new FileInfo(_savePath);
                FileSize = fi.Length;
                Status = BackupTaskStatus.Completed;
                var msg = $"{LocalizeHelper.GetString("String_Backup_process_completed")} -> {_savePath}";
                StatusHelper.UpdateStatus(msg);
            }
            else
            {
                if (File.Exists(_savePath)) { File.Delete(_savePath); }
                Status = BackupTaskStatus.Failed;
                var msg = $"{LocalizeHelper.GetString("String_Backup_process_failed")} : {ex.Message}";
                StatusHelper.UpdateStatus(msg);
            }
        }

        /// <summary>
        /// スレッド完了後にOnBackupCompleted()実行と
        /// cTokenSourceの破棄
        /// </summary>
        private void ThreadCompletedCallback()
        {
            StatusHelper.RequestUnlockSetting();
            StatusHelper.SetProgressStatus(false);
            OnBackupCompleted();
            cTokenSource?.Dispose();
            cTokenSource = null;
            Debug.WriteLine($"Backup -> {_savePath}");
        }

        private void ProgressChangedCallback(object sender, SimpleBackup.Events.ProgressChangedEventArgs e)
        {
            Progress = e.Progress;
        }

        private void OnBackupCompleted()
        {
            BackupCompleted?.Invoke(this, new BackupCompletedEvent(this));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RequestCancel()
        {
            cTokenSource?.Cancel();
        }

        ~BackupTask()
        {
            cTokenSource?.Cancel();
        }

        private void CancelCommandExecute(object parameter)
        {
            RequestCancel();
        }

        private bool CancelCommandCanExecute(object parameter)
        {
            return Status == BackupTaskStatus.Processing;
        }

        private ICommand _cancelCommand;
        [JsonIgnore]
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                    _cancelCommand = new DelegateCommand
                    {
                        ExecuteHandler = CancelCommandExecute,
                        CanExecuteHandler = CancelCommandCanExecute,
                    };
                return _cancelCommand;
            }
        }
    }
}
