using SimpleBackup.Properties;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SimpleBackup
{
    /// <summary>
    /// MainWindow用ViewModel
    /// </summary>
    internal class ViewModel : INotifyPropertyChanged
    {
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
                    BackupTask = new BackupTask(
                        BackupTargetDir,
                        SaveDir,
                        BackupInterval
                    );
                }
                else
                {
                    BackupTask?.StopTimer();
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

        //バックアップ実施履歴
        public ObservableCollection<BackupHistoryEntry> BackupHistory { get; set; } = new ObservableCollection<BackupHistoryEntry>();

        //実行中のバックアップ処理
        private BackupTask _backupTask;
        public BackupTask BackupTask
        {
            get { return _backupTask; }
            set
            {
                _backupTask?.StopTimer();
                _backupTask = value;
                if (_backupTask != null)
                {
                    _backupTask.BackupCompleted += new BackupCompletedEventHandler(BackupTask_Completed);
                    _backupTask.BackupNow();
                }
            }
        }

        private void BackupTask_Completed(object sender, BackupCompletedEventArgs e)
        {
            for (int i = 0; i < BackupHistory.Count;)
            {
                var entry = BackupHistory[i];

                if (entry.Index != int.MaxValue)
                    entry.Index++;

                if (entry.Index >= MaxBackups && entry.IsSequence == true)
                {
                    RemoveBackup(BackupHistory[i]);
                }
                else
                {
                    i++;
                }
            }
            BackupHistory.Add(new BackupHistoryEntry
            {
                Index = 0,
                SourcePath = e.SourcePath,
                SaveDir = e.SaveDir,
                FileName = e.BackupName
            });
        }

        public void RemoveBackup(BackupHistoryEntry entry)
        {
            try
            {
                var path = Path.Combine(entry.SaveDir, entry.FileName);
                if (File.Exists(path) == true)
                {
                    File.Delete(path);
                }
                BackupHistory.Remove(entry);
            }
            catch
            { }
        }

        private void ResetSequence()
        {
            int index = 0;

            foreach (BackupHistoryEntry entry in
                BackupHistory.OrderByDescending<BackupHistoryEntry, DateTime>(BackupHistoryEntry => BackupHistoryEntry.SaveTime)
            )
            {
                if (
                    entry.SourcePath == BackupTargetDir &&
                    entry.SaveDir == SaveDir
                )
                {
                    entry.IsSequence = true;
                    entry.Index = index;
                    index++;
                }
                else
                {
                    entry.IsSequence = false;
                    entry.Index = int.MaxValue;
                }
            }
        }

        public ViewModel()
        {
        }
    }
}
