using SimpleBackup.Properties;
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

namespace SimpleBackup
{
    /// <summary>
    /// MainWindow用ViewModel
    /// </summary>
    internal class ViewModel : INotifyPropertyChanged
    {
        private readonly static ViewModel _instance = new ViewModel();
        public static ViewModel Instance { get { return _instance; } }

        private ViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(BackupHistory, _backupHistorySync);
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
                    ResetSequence();
                }
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
                    ResetSequence();
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

        public ProcessPriorityClass ProcessPriority
        {
            get
            {
                return Process.GetCurrentProcess().PriorityClass;
            }
            set
            {
                Process.GetCurrentProcess().PriorityClass = value;
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
                        JsonHelper.SerializeToFile(BackupHistory, SaveDir);
                    }
                }
            });
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

            BackupTask bt = new BackupTask
            {
                Index = -1,
                SourcePath = sourcePath,
                SaveDir = saveDir,
                FileName = saveName,
                InSequence = true
            };
            bt.BackupCompleted += new BackupCompletedEventHandler(BackupTask_Completed);
            await Task.Run(() =>
            {
                lock (_backupHistoryLockObj)
                {
                    BackupHistory.Add(bt);
                }
            });

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

        public async void LoadBackupHistory()
        {
            var loadedCollection = await JsonHelper.DeserializeFromFile<List<BackupTask>>(SaveDir);

            if (loadedCollection?.Count >= 1)
            {
                await Task.Run(() =>
                {
                    lock (_backupHistoryLockObj)
                    {
                        BackupHistory.Clear();
                        BackupHistory.AddRange(loadedCollection);
                    }
                });
            }

            ResetSequence();
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
