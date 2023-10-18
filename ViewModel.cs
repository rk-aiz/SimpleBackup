using SimpleBackup.Converters;
using SimpleBackup.Events;
using SimpleBackup.Extensions;
using SimpleBackup.Helpers;
using SimpleBackup.Properties;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SimpleBackup
{
    /// <summary>
    /// ViewModel for MainWindow
    /// </summary>
    internal class ViewModel : INotifyPropertyChanged
    {
        private readonly static ViewModel _instance = new ViewModel();
        public static ViewModel Instance { get { return _instance; } }
        public Dispatcher Dispatcher { get; private set; } = Dispatcher.CurrentDispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ViewModel()
        {
            CBTSource = new FileSystemTreeNode(Dispatcher, true);
            BindingOperations.EnableCollectionSynchronization(BackupHistory, _backupHistorySync);
            MeasureBackupTargetDir();
            UpdateDestinationDirveInfo();
            LoadBackupHistory();
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
                    StatusHelper.RequestLockSetting();
                    SetupBackupScheduler();
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
                    //SaveBackupHistory();
                    MeasureBackupTargetDir();
                    LoadBackupHistory();
                }
            }
        }

        private long _backupTargetTotalLength;
        public long BackupTargetTotalLength
        {
            get { return _backupTargetTotalLength; }
            set { _backupTargetTotalLength = value; OnPropertyChanged("BackupTargetTotalLength"); }
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

        public FileSystemTreeNode CBTSource { get; private set; }

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
                    Debug.WriteLine($"MaxBackups : {Settings.Default.Maximum_Number_of_Backups}");
                    OnPropertyChanged("MaxBackups");
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
                var dm = new DirectoryMeasure(BackupTargetDir, _measureBTDirCancelTSource.Token, (sender, e) =>
                {
                    BackupTargetTotalLength = e.NewLength;
                    BackupTargetFilesCount = e.NewCount;
                });
                BackupTargetTotalLength = dm.GetTotalSize();
                BackupTargetFilesCount = dm.GetTotalCount();
            }, _measureBTDirCancelTSource.Token);

            //Debug.WriteLine($"MeasureBackupTargetDir {CBTSource.Children.Count}");
            await Dispatcher.InvokeAsync(() =>
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

        public async void SetupBackupScheduler()
        {
            //バックアップ対象が存在しない場合中止
            if (String.IsNullOrWhiteSpace(BackupTargetDir) || Directory.Exists(BackupTargetDir) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_target_does_not_exist"));
                SchedulerEnabled = false;
                return;
            }

            //バックアップ保存場所が存在しない場合中止
            if (String.IsNullOrWhiteSpace(SaveDir) || Directory.Exists(SaveDir) == false)
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Save_Location_does_not_exist"));
                SchedulerEnabled = false;
                return;
            }

            var targetList = await CBTSource.GetCheckedItemsAsync();

            if (targetList != null && ValidateTargetLength(targetList.TotalLength))
            {
                BackupScheduler = new BackupScheduler(
                    BackupTargetDir,
                    SaveDir,
                    BackupInterval
                );
            }
            else
            {
                StatusHelper.UpdateStatus(LocalizeHelper.GetString("String_Backup_Task_Canceled"));
                SchedulerEnabled = false;
            }
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

        public void CreateBackupTask()
        {
            CreateBackupTask(BackupTargetDir, SaveDir);
        }

        public async void CreateBackupTask(string sourcePath, string saveDir, bool force = false)
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

            var di = new DirectoryInfo(sourcePath);
            var name = di.Name.Trim(Path.VolumeSeparatorChar, Path.DirectorySeparatorChar);
            var saveName = $"{name}-{DateTime.Now:yyyyMMdd-HH-mm-ss}.zip";
            var savePath = System.IO.Path.Combine(saveDir, saveName);

            //同名バックアップファイルが既に存在する場合中止
            if (File.Exists(savePath)) { return; }

            //バックアップ処理中は設定変更できないようにする
            StatusHelper.RequestLockSetting();

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
                lock (_backupHistorySync)
                {
                    BackupHistory.Add(bt);
                }
            });

            //バックアップ項目をリストアップ
            bt.TargetList = await CBTSource.GetCheckedItemsAsync();

            bool validation = false;
            if (!force)
            {
                if (bt.TargetList != null)
                {
                    validation = ValidateTargetLength(bt.TargetList.TotalLength);
                }
            }
            else
            {
                validation = true;
            }

            await task;

            if (validation)
            {
                bt.Backup();
            }
            else
            {
                bt.RequestCancel();
            }
        }

        public float ThreatholdLength
        {
            get { return (float)Settings.Default.ThreatholdLength; ; }
            set { Settings.Default.ThreatholdLength = (double)value; OnPropertyChanged(nameof(ThreatholdLength)); }
        }

        public bool TargetSizeValidation
        {
            get { return Settings.Default.TargetSizeValidation; }
            set { Settings.Default.TargetSizeValidation = value; OnPropertyChanged(nameof(TargetSizeValidation)); }
        }
        
        private bool ValidateTargetLength(long length)
        {
            if (length > _destinationDriveAvailableFreeSpace)
            {
                string message = LocalizeHelper.GetString("String_Free_Space_Check") + "\n";
                message += $"{LocalizeHelper.GetString("String_Target_Size")} : {LengthToByteStringConverter.LengthToByteString(length)}";
                string caption = LocalizeHelper.GetString("Confirm"); ;
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);

                return false;
            }
            else if (TargetSizeValidation && length > ThreatholdLength)
            {
                // Initializes the variables to pass to the MessageBox.Show method.
                string message = LocalizeHelper.GetString("String_Target_Size_Very_Large") + "\n";
                message += LocalizeHelper.GetString("String_Do_You_Want_To_Continue") + "\n";
                message += $"{LocalizeHelper.GetString("String_Target_Size")} : {LengthToByteStringConverter.LengthToByteString(length)}";
                string caption = LocalizeHelper.GetString("Confirm");
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                DialogResult result;

                result = MessageBox.Show(message, caption, buttons);
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return false;
                }
            }

            return true;
        }

        public void RemoveFailedBackup()
        {
            var source = BackupHistory.Where(entry => entry.InSequence && entry.Status == BackupTaskStatus.Failed);
            Parallel.ForEach(source, entry => RemoveBackup(entry));
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
                    await Dispatcher.InvokeAsync(() =>
                    {
                        lock (_backupHistoryLockObj)
                        {
                            BackupHistory.Remove(entry);
                        }
                    }, DispatcherPriority.DataBind);
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
                await Dispatcher.InvokeAsync(() =>
                {
                    lock (_backupHistoryLockObj)
                    {
                        BackupHistory.Clear();
                        BackupHistory.AddRange(jsObj.BackupHistory);
                    }
                }, DispatcherPriority.DataBind);
            }
            ResetSequence();

            CBTSource.SetIgnoreItems(jsObj.IgnoreItems);
        }

        /// <summary>
        /// Jsonファイルにバックアップ履歴とファイル別設定を書き込みます
        /// </summary>
        public async void SaveBackupHistory()
        {
            if (string.IsNullOrWhiteSpace(SaveDir)) { return; }

            bool serializeFlag = false;
            var jsObj = new JsonObject();
            if (BackupHistory != null && BackupHistory.Count != 0)
            {
                serializeFlag = true;
                jsObj.BackupHistory.AddRange(BackupHistory);
            }

            var ignoreItems = CBTSource.GetIgnoreItems();
            if (ignoreItems != null && ignoreItems.Count != 0)
            {
                serializeFlag = true;
                jsObj.IgnoreItems.AddRange(ignoreItems);
            }

            //foreach (var item in jsObj.IgnoreItems) { Debug.WriteLine(item); }
            if (serializeFlag)
            {
                await jsObj.SerializeToFileAsync(SaveDir);
            }
        }

        private void ResetSequence()
        {
            int index = 0;

            foreach (BackupTask entry in
                BackupHistory.OrderByDescending<BackupTask, DateTime>(bt => bt.SaveTime)
            )
            {
                if (entry.SourcePath != BackupTargetDir ||
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

        public void Refresh()
        {
            var ignoreItems = CBTSource.GetIgnoreItems();
            MeasureBackupTargetDir();
            UpdateDestinationDirveInfo();

            Dispatcher.DoEvents(DispatcherPriority.Render);

            CBTSource.SetIgnoreItems(ignoreItems);
        }

        private async void BackupTask_Completed(object sender, BackupCompletedEvent e)
        {
            if (e.BackupTask.Status == BackupTaskStatus.Failed)
            {
                e.BackupTask.Index = -1;
                return;
            }

            if (e.BackupTask.SaveDir != SaveDir) { return; }

            var li = ArrayList.Synchronized(new ArrayList(BackupHistory.Count));
            //シークエンス中のバックアップ履歴のIndexをインクリメント
            //Indexが最大バックアップ保存件数以上の場合Remove
            await Task.Run(() =>
            {
                //ループ中にBackupHistory.Countが変わらないようにロック
                lock (_backupHistoryLockObj)
                {
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
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (BackupTask entry in li)
                {
                    RemoveBackup(entry);
                }
            }, DispatcherPriority.DataBind);

            SaveBackupHistory();
            UpdateDestinationDirveInfo();
        }
    }
}
