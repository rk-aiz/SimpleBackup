using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SimpleBackup.Models;

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

        private bool _InSequence = true;
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
            set { _status = value; OnPropertyChanged("Status"); }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        //バックアップ処理中のロック
        private CancellationTokenSource cTokenSource;

        //バックアップ終了時のイベント
        public event BackupCompletedEventHandler BackupCompleted;

        public BackupTask()
        {
            _saveTime = DateTime.Now;
            _status = BackupTaskStatus.Processing;
        }

        private Task CreateZipAsync(DirectoryInfo src, string path, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    var zah = new ZipArchiveHelper(src, path, token);
                    zah.ProgressChanged += (s, e) =>
                    {
                        Progress = e.Progress;
                    };
                    zah.CreateZipArchive();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

        public async void Backup()
        {
            //バックアップ処理が既に実行中の場合中止
            if (cTokenSource != null) { return; }

            //ステータスバー更新
            StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_process_in_progress"));
            StatusHelper.SetProgressStatus(true);

            //Destination Path
            var savePath = System.IO.Path.Combine(SaveDir, FileName);

            using (cTokenSource = new CancellationTokenSource())
            {
                DirectoryInfo diTarget = new DirectoryInfo(SourcePath);
                try
                {
                    await CreateZipAsync(diTarget, savePath, cTokenSource.Token);

                    Status = BackupTaskStatus.Completed;
                    var msg = $"{LocalizeHelper.GetString("String_Backup_process_completed")} -> {savePath}";
                    StatusHelper.UpdateStatus(msg);
                }
                catch (Exception ex)
                {
                    if (File.Exists(savePath)) { File.Delete(savePath); }
                    Status = BackupTaskStatus.Failed;
                    var msg = $"{LocalizeHelper.GetString("String_Backup_process_failed")} : {ex.Message}";
                    StatusHelper.UpdateStatus(msg);
                }
                finally
                {
                    StatusHelper.SetProgressStatus(false);
                    OnBackupCompleted();
                    Debug.WriteLine($"Backup -> {savePath}");
                }
            }
            cTokenSource = null;
        }

        private void OnBackupCompleted()
        {
            BackupCompleted?.Invoke(this, new BackupCompletedEventArgs(this));
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
    }
}
