using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using SimpleBackup.Helpers;
using SimpleBackup.Properties;
using SimpleBackup.Extensions;

namespace SimpleBackup
{
    /// <summary>
    /// ViewModel for MainWindow
    /// </summary>
    internal class ViewModel : INotifyPropertyChanged
    {
        private readonly static ViewModel _instance = new ViewModel();
        public static ViewModel Instance { get { return _instance; } }

        private ViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(BackupHistory, _backupHistorySync);
            MeasureBackupTargetDir();
            UpdateDestinationDirveInfo();
            LoadBackupHistory();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //定期バックアップの有効/無効
        private bool _schedulerEnabled;
        public bool SchedulerEnabled
        {
            get { return _schedulerEnabled; }
            set
            {
                _schedulerEnabled = value;

                if (_schedulerEnabled == true)
                {
                    BackupScheduler = new BackupScheduler(
                        BackupTargetDir,
                        SaveDir,
                        BackupInterval
                    );
                    StatusHelper.RequestLockSetting();
                }
                else
                {
                    BackupScheduler?.StopTimer();
                    StatusHelper.RequestUnlockSetting();
                }

                OnPropertyChanged("SchedulerEnabled");
            }
        }

        //バックアップ対象のディレクトリ
        public string BackupTargetDir
        {
            get { return Settings.Default.Target_Directory; }
            set
            {
                if (Settings.Default.Target_Directory != value)
                {
                    Settings.Default.Target_Directory = value;
                    Debug.WriteLine($"BackupTargetDir : {Settings.Default.Target_Directory}");
                    StatusHelper.UpdateStatus($"{LocalizeHelper.GetString("String_Backup_Source")} -> {Settings.Default.Target_Directory}");
                    OnPropertyChanged("BackupTargetDir");
                    SaveBackupHistory();
                    MeasureBackupTargetDir();
                    LoadBackupHistory();
                }
            }
        }

        private long _backupTargetTotalLength;
        public long BackupTargetTotalLength
        {
            get { return _backupTargetTotalLength; }
            set { _backupTargetTotalLength  = value; OnPropertyChanged("BackupTargetTotalLength"); }
        }

        private int _backupTargetFilesCount;
        public int BackupTargetFilesCount
        {
            get { return _backupTargetFilesCount; }
            set { _backupTargetFilesCount = value; OnPropertyChanged("BackupTargetFilesCount"); }
        }

        private long _destinationDriveTotalSize;
        public long DestinationDriveTotalSize
        {
            get { return _destinationDriveTotalSize; }
            set { _destinationDriveTotalSize = value; OnPropertyChanged("DestinationDriveTotalSize"); }
        }

        private long _destinationDriveAvailableFreeSpace;
        public long DestinationDriveAvailableFreeSpace
        {
            get { return _destinationDriveAvailableFreeSpace; }
            set { _destinationDriveAvailableFreeSpace = value; OnPropertyChanged("DestinationDriveAvailableFreeSpace"); }
        }

        public FileSystemTreeNode CBTSource { get; } = new FileSystemTreeNode();

        public double DriveFreeSpacePercentage
        {
            get
            {
                var val = 1.0 - ((float)_destinationDriveAvailableFreeSpace / (float)_destinationDriveTotalSize);
                return val;
            }
        }

        //バックアップ保存場所のディレクトリ
        public string SaveDir
        {
            get { return Settings.Default.Save_Directory; }
            set
            {
                if (Settings.Default.Save_Directory != value)
                {
                    Settings.Default.Save_Directory = value;
                    StatusHelper.UpdateStatus($"{LocalizeHelper.GetString("String_Save_Location")} -> {Settings.Default.Save_Directory}");
                    Debug.WriteLine($"SaveDir : {Settings.Default.Save_Directory}");
                    OnPropertyChanged("SaveDir");
                    LoadBackupHistory();
                    UpdateDestinationDirveInfo();
                }
            }
        }

        //定期バックアップのインターバル
        public uint BackupInterval
        {
            get { return Settings.Default.Backup_Interval; }
            set
            {
                if (Settings.Default.Backup_Interval != value)
                {
                    Settings.Default.Backup_Interval = value;
                    var msg = $"{LocalizeHelper.GetString("String_Backup_Interval")} -> {Settings.Default.Backup_Interval} {LocalizeHelper.GetString("String_Min")}";
                    StatusHelper.UpdateStatus(msg);
                    Debug.WriteLine($"BackupInterval : {Settings.Default.Backup_Interval}");
                    OnPropertyChanged("BackupInterval");
                }
            }
        }

        //バックアップ数に制限を決めて、それ以上になると古いバックアップを削除する
        public uint MaxBackups
        {
            get { return Settings.Default.Maximum_Number_of_Backups; }
            set
            {
                if (Settings.Default.Maximum_Number_of_Backups != value)
                {
                    Settings.Default.Maximum_Number_of_Backups = value;
                    var msg = $"{LocalizeHelper.GetString("String_Maximum_Number_of_Backups")} -> {Settings.Default.Maximum_Number_of_Backups}";
                    StatusHelper.UpdateStatus(msg);
                    Debug.WriteLine($"maxBackups : {Settings.Default.Maximum_Number_of_Backups}");
                    OnPropertyChanged("maxBackups");
                }
            }
        }

        public Priority Priority
        {
            get
            {
                return (Priority)Settings.Default.Thread_Priority;
            }
            set
            {
                Debug.WriteLine(value);
                Settings.Default.Thread_Priority = (int)value; OnPropertyChanged("Priority");
            }
        }

        //バックアップ実施履歴
        public ObservableCollection<BackupTask> BackupHistory { get; } = new ObservableCollection<BackupTask>();

        // 別スレッドでループ処理をする時にコレクションのサイズが変わらないようにする
        // (EnableCollectionSynchronizationは適用していないので別スレッドからのコレクション変更はできない)
        private readonly object _backupHistoryLockObj = new object();
        private readonly object _backupHistorySync = new object();

        //実行中のバックアップ処理
        private BackupScheduler _backupScheduler;
        public BackupScheduler BackupScheduler
        {
            get { return _backupScheduler; }
            set
            {
                _backupScheduler?.StopTimer();
                _backupScheduler = value;
            }
        }

        private CancellationTokenSource _measureBTDirCancelTSource;
        private async void MeasureBackupTargetDir()
        {
            _measureBTDirCancelTSource?.Cancel();
            _measureBTDirCancelTSource = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                var dm = new DirectoryMeasure(BackupTargetDir, _measureBTDirCancelTSource.Token);
                BackupTargetTotalLength = dm.GetTotalSize();
                BackupTargetFilesCount = dm.GetTotalCount();
            }, _measureBTDirCancelTSource.Token);

            //Debug.WriteLine($"MeasureBackupTargetDir {CBTSource.Children.Count}");
            await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (Directory.Exists(BackupTargetDir))
                {
                    CBTSource.Name = BackupTargetDir;
                    CBTSource.IsChecked = true;
                    CBTSource.GetChildren(new DirectoryInfo(BackupTargetDir));

                }
            });
            await task;
        }

        private void UpdateDestinationDirveInfo()
        {
            try
            {
                var dirInfo = new DirectoryInfo(SaveDir);
                var driveInfo = new DriveInfo(dirInfo.Root.Name);
                DestinationDriveTotalSize = driveInfo.TotalSize;
                DestinationDriveAvailableFreeSpace = driveInfo.AvailableFreeSpace;
                OnPropertyChanged("DriveFreeSpacePercentage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void BackupTask_Completed(object sender, BackupCompletedEventArgs e)
        {
            if (e.BackupTask.Status == BackupTaskStatus.Failed)
            {
                e.BackupTask.Index = -1;
                return;
            }

            //シークエンス中のバックアップ履歴のIndexをインクリメント
            //Indexが最大バックアップ保存件数以上の場合Remove
            await Task.Run(() =>
            {
                //ループ中にBackupHistory.Countが変わらないようにロック
                lock (_backupHistoryLockObj)
                {
                    if (e.BackupTask.SaveDir == SaveDir)
                    {
                        List<BackupTask> li = new List<BackupTask>();
                        Parallel.ForEach(BackupHistory, entry =>
                        {
                            if (entry.InSequence == true &&
                                entry.Status == BackupTaskStatus.Completed)
                            {
                                entry.Index++;
                            }

                            if (entry.InSequence == true && entry.Index >= MaxBackups)
                            {
                                li.Add(entry);
                            }
                        });

                        Parallel.ForEach(li, entry => RemoveBackup(entry, true));
                        SaveBackupHistory();
                    }
                }
            });

            UpdateDestinationDirveInfo();
        }

        public void CreateBackupTask()
        {
            CreateBackupTask(BackupTargetDir, SaveDir);
        }

        public async void CreateBackupTask(string sourcePath, string saveDir)
        {
            //バックアップ対象が存在しない場合中止
            if (String.IsNullOrWhiteSpace(sourcePath) || Directory.Exists(sourcePath) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_target_does_not_exist"));
                return;
            }

            //バックアップ保存場所が存在しない場合中止
            if (String.IsNullOrWhiteSpace(saveDir) || Directory.Exists(saveDir) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Save_Location_does_not_exist"));
                return;
            }

            var saveName = $"{Path.GetFileName(sourcePath)}-{DateTime.Now:yyyyMMdd-hh-mm-ss}.zip";
            var savePath = System.IO.Path.Combine(saveDir, saveName);

            //同名バックアップファイルが既に存在する場合中止
            if (File.Exists(savePath)) { return; }

            RemoveFailedBackup();

            BackupTask bt = new BackupTask((ThreadPriority)Priority)
            {
                Index = -1,
                SourcePath = sourcePath,
                SaveDir = saveDir,
                FileName = saveName,
                InSequence = true
            };
            bt.BackupCompleted += new BackupCompletedEventHandler(BackupTask_Completed);
            var task = Task.Run(() =>
            {
                lock (_backupHistoryLockObj)
                {
                    BackupHistory.Add(bt);
                }
            });

            //バックアップ項目をリストアップ
            bt.TargetItems = await Dispatcher.CurrentDispatcher.InvokeAsync(() => CBTSource.GetCheckedItems());

            await task;
            bt.Backup();
        }

        public void RemoveFailedBackup()
        {
            for (int n = 0; n < BackupHistory.Count;)
            {
                var entry = BackupHistory[n];

                if (entry.InSequence == true &&
                    entry.Status == BackupTaskStatus.Failed)
                {
                    RemoveBackup(entry);
                }
                else
                {
                    n++;
                }
            }
        }

        public async void RemoveBackup(BackupTask entry, bool noLock = false)
        {
            try
            {
                var path = Path.Combine(entry.SaveDir, entry.FileName);
                if (File.Exists(path) == true)
                {
                    File.Delete(path);
                }
                if (noLock)
                {
                    BackupHistory.Remove(entry);
                }
                else
                {
                    await Task.Run(() =>
                    {
                        lock (_backupHistoryLockObj)
                        {
                            BackupHistory.Remove(entry);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Jsonファイルからバックアップ履歴とファイル別設定を読み込みます
        /// </summary>
        public async void LoadBackupHistory()
        {
            if (SaveDir == null) { return; }

            var jsObj = new JsonObject();
            try
            {
                await jsObj.DeserializeFromFileAsync(SaveDir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (jsObj.BackupHistory?.Count >= 1)
            {
                await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    lock (_backupHistoryLockObj)
                    {
                        BackupHistory.Clear();
                        BackupHistory.AddRange(jsObj.BackupHistory);
                    }
                });
            }
            ResetSequence();

            CBTSource.SetIgnoreItems(jsObj.IgnoreItems);
        }

        /// <summary>
        /// Jsonファイルにバックアップ履歴とファイル別設定を書き込みます
        /// </summary>
        public async void SaveBackupHistory()
        {
            var jsObj = new JsonObject();
            jsObj.BackupHistory = new List<BackupTask>(BackupHistory);
            jsObj.IgnoreItems = CBTSource.GetIgnoreItems();

            //foreach (var item in jsObj.IgnoreItems) { Debug.WriteLine(item); }
            await jsObj.SerializeToFileAsync(SaveDir);
        }

        private void ResetSequence()
        {
            int index = 0;

            foreach (BackupTask entry in
                BackupHistory.OrderByDescending<BackupTask, DateTime>(bt => bt.SaveTime)
            )
            {
                if (
                    entry.SourcePath != BackupTargetDir ||
                    entry.SaveDir != SaveDir
                )
                {
                    entry.InSequence = false;
                    entry.Index = int.MaxValue;
                }
                else if (entry.Status == BackupTaskStatus.Completed)
                {
                    entry.InSequence = true;
                    entry.Index = index;
                    index++;
                }
                else
                {
                    entry.InSequence = true;
                    entry.Index = -1;
                }
            }
        }
    }
}
