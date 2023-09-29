using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SimpleBackup
{
    internal class ViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _scheduleEnabled;
        public bool ScheduleEnabled
        {
            get { return _scheduleEnabled; }
            set
            {
                _scheduleEnabled = value;

                if (_scheduleEnabled == true)
                {
                    BackupTask = new BackupTask(
                        BackupTargetDir,
                        SaveDir,
                        BackupInterval
                    );
                }
                else
                {
                    BackupTask?.Dispose();
                }

                OnPropertyChanged("ScheduleEnabled");
            }
        }

        private string _backupTargetDir;
        public string BackupTargetDir
        {
            get { return _backupTargetDir; }
            set
            {
                if (_backupTargetDir != value)
                {
                    _backupTargetDir = value;
                    Debug.WriteLine($"BackupTargetDir : {_backupTargetDir}");
                    StatusHelper.UpdateStatus($"{LocalizeHelper.GetString("String_Backup_Source")} -> {_backupTargetDir}");
                    OnPropertyChanged("BackupTargetDir");
                    ChangeSequence();
                }
            }
        }

        private string _saveDir;
        public string SaveDir
        {
            get { return _saveDir; }
            set
            {
                if (_saveDir != value)
                {
                    _saveDir = value;
                    StatusHelper.UpdateStatus($"{LocalizeHelper.GetString("String_Save_Location")} -> {_saveDir}");
                    Debug.WriteLine($"SaveDir : {_saveDir}");
                    OnPropertyChanged("saveDir");
                    ChangeSequence();
                }
            }
        }

        private uint _backupInterval = 10;
        public uint BackupInterval
        {
            get { return _backupInterval; }
            set
            {
                if (_backupInterval != value)
                {
                    _backupInterval = value;
                    var msg = $"{LocalizeHelper.GetString("String_Backup_Interval")} -> {_backupInterval} {LocalizeHelper.GetString("String_Min")}";
                    StatusHelper.UpdateStatus(msg);
                    Debug.WriteLine($"BackupInterval : {_backupInterval}");
                    OnPropertyChanged("backupInterval");
                }
            }
        }

        private uint _maxBackups;
        public uint MaxBackups
        {
            get { return _maxBackups; }
            set
            {
                if (_maxBackups != value)
                {
                    _maxBackups = value;
                    var msg = $"{LocalizeHelper.GetString("String_Maximum_Number_of_Backups")} -> {_maxBackups}";
                    StatusHelper.UpdateStatus(msg);
                    Debug.WriteLine($"maxBackups : {_maxBackups}");
                    OnPropertyChanged("maxBackups");
                }
            }
        }

        public ObservableCollection<BackupHistoryEntry> BackupHistory { get; set; } = new ObservableCollection<BackupHistoryEntry>();

        private BackupTask _backupTask;
        public BackupTask BackupTask
        {
            get { return _backupTask; }
            set
            {
                _backupTask?.Dispose();
                _backupTask = value;
                if (_backupTask != null)
                {
                    _backupTask.BackupCompleted += new BackupTask.BackupCompletedEventHandler(BackupTask_Completed);
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

                if (
                    entry.Index >= MaxBackups &&
                    entry.SourcePath == BackupTargetDir &&
                    entry.SaveDir == SaveDir
                )
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
                FileInfo fi = new FileInfo(path);
                if (fi.Exists == true)
                {
                    fi.Delete();
                }
                BackupHistory.Remove(entry);
            }
            catch
            { }
        }

        private void ChangeSequence()
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
            try
            {
                BackupTargetDir = Properties.Settings.Default.Target_Directory;
                SaveDir = Properties.Settings.Default.Save_Directory;
                BackupInterval = Properties.Settings.Default.Backup_Interval;
                MaxBackups = Properties.Settings.Default.Maximum_Number_of_Backups;
            }
            catch { }

            /*
            var cv = CollectionViewSource.GetDefaultView(BackupHistory);
            cv.SortDescriptions.Clear();
            cv.SortDescriptions.Add(new SortDescription("IsSequence", ListSortDirection.Descending));
            cv.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
            */
        }

        public void Dispose()
        {
            Debug.WriteLine("ViewModel's disposer called");
            try
            {
                Properties.Settings.Default.Target_Directory = BackupTargetDir;
                Properties.Settings.Default.Save_Directory = SaveDir;
                Properties.Settings.Default.Backup_Interval = BackupInterval;
                Properties.Settings.Default.Maximum_Number_of_Backups = MaxBackups;

                Properties.Settings.Default.Save();
            }
            catch { }

            _backupTask?.Dispose();

        }
    }
}
