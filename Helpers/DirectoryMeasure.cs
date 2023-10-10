using System;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;

namespace SimpleBackup.Helpers
{
    /// <summary>
    /// ディレクトリのサイズ・ファイル数を累算する
    /// </summary>
    internal class DirectoryMeasure
    {
        private long totalSize = 0;
        private int totalCount = 0;

        private CancellationToken cToken;

        public DirectoryMeasure(string dirPath, CancellationToken? token = null)
        {
            if (String.IsNullOrEmpty(dirPath)) { return; }

            if (token is CancellationToken ct) { cToken = ct; }

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
                if (cToken.IsCancellationRequested) { return; }
                totalSize += fi.Length;
                totalCount++;
            }

            foreach (DirectoryInfo child in di.EnumerateDirectories("*"))
            {
                if (cToken.IsCancellationRequested) { return; }
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
