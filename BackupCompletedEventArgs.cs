using System;

namespace SimpleBackup
{
    public delegate void BackupCompletedEventHandler(object sender, BackupCompletedEventArgs e);

    public class BackupCompletedEventArgs : EventArgs
    {
        public string SourcePath { get; private set; }
        public string SaveDir { get; private set; }
        public string BackupName { get; private set; }

        public BackupCompletedEventArgs(string _sourcePath, string _saveDir, string _backupPath)
        {
            SourcePath = _sourcePath;
            SaveDir = _saveDir;
            BackupName = _backupPath;
        }
    }
}
