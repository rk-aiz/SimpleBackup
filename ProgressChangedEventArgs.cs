using System;

namespace SimpleBackup
{
    internal delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);

    internal class ProgressChangedEventArgs : EventArgs
    {
        public int Progress { get; private set; }

        public ProgressChangedEventArgs(int progress)
        {
            Progress = progress;
        }
    }
}
