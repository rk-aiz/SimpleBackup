using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using System.Collections;
using System.Security.AccessControl;
using System.Security.Principal;
using SimpleBackup.Events;

namespace SimpleBackup.Helpers
{
    /// <summary>
    /// ディレクトリのサイズ・ファイル数を累算する
    /// </summary>
    internal class DirectoryMeasure
    {
        private long _totalSize = 0;
        private int _totalCount = 0;
        private int _maxThread = 8;
        private int _thread = 0;
        //private ParallelOptions _pOptions = new ParallelOptions() { MaxDegreeOfParallelism = 8 };

        public event DirectoryMeasureUpdatedEventHandler DirectoryMeasureUpdated;

        private CancellationToken cToken;

        public DirectoryMeasure(string dirPath, CancellationToken? token = null, DirectoryMeasureUpdatedEventHandler updateCallback = null)
        {
            if (String.IsNullOrEmpty(dirPath)) { return; }

            if (token is CancellationToken ct)
            {
                cToken = ct;
                //_pOptions.CancellationToken = ct;
            }

            if (updateCallback != null)
            {
                DirectoryMeasureUpdated += updateCallback;
            }

            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (!(di.Exists)) { return; }

            _maxThread = Environment.ProcessorCount;
            //_pOptions.MaxDegreeOfParallelism = Environment.ProcessorCount; // 最大実行数

            try
            {
                AccumulateChild(di);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void updateCallback(object sender, DirectoryMeasureUpdatedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public long GetTotalSize()
        {
            return _totalSize;
        }

        public int GetTotalCount()
        {
            return _totalCount;
        }

        public void AccumulateChild(DirectoryInfo di)
        {
            if (cToken.IsCancellationRequested) { return; }

            Interlocked.Add(ref _totalSize, di.EnumerateFiles().Select(fi => fi.Length).Sum());
            Interlocked.Add(ref _totalCount, di.EnumerateFiles().Count());

            OnUpdateDirectoryMeasure();

            IEnumerable<DirectoryInfo> directories = di.EnumerateDirectories();

            if (directories.Take(2)?.Count() > 1 && _thread <= _maxThread)
            {
                Parallel.ForEach(directories, child =>
                {
                    Interlocked.Increment(ref _thread);
                    try
                    {
                        AccumulateChild(child);
                    }
                    catch { }
                });
            }
            else
            {
                foreach (DirectoryInfo child in directories)
                {
                    try
                    {
                        AccumulateChild(child);
                    }
                    catch { }
                }
            }
        }

        private DateTime _lastUpdateTime = DateTime.MinValue;
        private TimeSpan _updateThreashold = TimeSpan.FromSeconds(2);
        private void OnUpdateDirectoryMeasure()
        {
            if (DateTime.Now - _lastUpdateTime > _updateThreashold)
            {
                _lastUpdateTime = DateTime.Now;
                DirectoryMeasureUpdated?.Invoke(this, new DirectoryMeasureUpdatedEventArgs(_totalSize, _totalCount));
            }
        }
    }
}
