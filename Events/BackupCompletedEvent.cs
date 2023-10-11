using System;

namespace SimpleBackup.Events
{
    internal delegate void BackupCompletedEventHandler(object sender, BackupCompletedEvent e);

    internal class BackupCompletedEvent : EventArgs
    {
        public BackupTask BackupTask { get; set; }

        public BackupCompletedEvent(BackupTask bt)
        {
            BackupTask = bt;
        }
    }
}
