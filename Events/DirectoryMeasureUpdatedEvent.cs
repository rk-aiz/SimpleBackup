﻿using System;

namespace SimpleBackup.Events
{
    internal delegate void DirectoryMeasureUpdatedEventHandler(object sender, DirectoryMeasureUpdatedEventArgs e);

    internal class DirectoryMeasureUpdatedEventArgs : EventArgs
    {
        public long NewLength { get; private set; }
        public int NewCount { get; private set; }

        public DirectoryMeasureUpdatedEventArgs(long newLength, int newCount)
        {
            NewLength = newLength;
            NewCount = newCount;
        }
    }
}
