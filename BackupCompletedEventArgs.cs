using System;

namespace SimpleBackup
{
    internal delegate void BackupCompletedEventHandler(object sender, BackupCompletedEventArgs e);

    internal class BackupCompletedEventArgs : EventArgs
    {
        public BackupTask BackupTask { get; set; }

        public BackupCompletedEventArgs(BackupTask bt)
        {
            BackupTask = bt;
        }
    }
}
