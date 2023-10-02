using System;
using System.Diagnostics;
using System.IO;

namespace SimpleBackup
{
    internal class DirectoryMeasure
    {
        private long totalSize = 0;
        private int totalCount = 0;

        public DirectoryMeasure(string dirPath)
        {
            if (String.IsNullOrEmpty(dirPath)) { return; }

            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (!(di.Exists)) { return; }

            try
            {
                AccumulateChild(di);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public long GetTotalSize()
        {
            return totalSize;
        }

        public int GetTotalCount()
        {
            return totalCount;
        }

        public void AccumulateChild(DirectoryInfo di)
        {
            foreach (FileInfo fi in di.EnumerateFiles("*"))
            {
                totalSize += fi.Length;
                totalCount++;
            }

            foreach (DirectoryInfo child in di.EnumerateDirectories("*"))
            {
                try
                {
                    AccumulateChild(child);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}
