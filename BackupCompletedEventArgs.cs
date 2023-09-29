using System;

namespace SimpleBackup
{
    class BackupCompletedEventArgs : EventArgs
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
