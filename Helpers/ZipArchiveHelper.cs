using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using SimpleBackup.Events;

namespace SimpleBackup.Helpers
{
    /// <summary>
    /// ディレクトリ配下のファイルとディレクトリを順次アーカイブに追加します。
    /// </summary>
    internal class ZipArchiveHelper
    {
        private ZipArchive _archive;

        public event ProgressChangedEventHandler ProgressChanged;
        private int _progress;

        //Zipファイルに作成した項目数(ファイル数)
        private int _entriesCount;
        private int _totalTargetFiles;
        private long _totalTargetDataSize;
        private long _completedDataSize;

        private int _bufferLength;

        private string _savePath;
        private DirectoryInfo _baseDir;
        private List<FileSystemInfo> _targetItems;

        private CancellationToken _cToken;

        /// <summary>
        /// Method that receives callbacks from threads upon completion.
        /// </summary>
        public delegate void ZipArchiveCompletedCallback(Exception ex);
        public ZipArchiveCompletedCallback ContinueWith { get; set; }

        /// <summary>
        /// ディレクトリからZipファイルを作成します
        /// </summary>
        /// <param name="di">ソースとなるディレクトリ</param>
        /// <param name="savePath">保存先</param>
        /// <param name="token">CancellationToken</param>
        public ZipArchiveHelper(DirectoryInfo baseDir, List<FileSystemInfo> targetItems, string savePath, CancellationToken token)
        {
            _savePath = savePath;
            _baseDir = baseDir;
            _cToken = token;
            _targetItems = targetItems;
            //進捗を計算するためにソースとなるファイルサイズを累算する
            DirectoryMeasure dm = new DirectoryMeasure(_baseDir.FullName, token);
            _totalTargetFiles = dm.GetTotalCount();
            _totalTargetDataSize = dm.GetTotalSize();
            //StreamReaderのバッファーサイズ
            _bufferLength = 16 * 1024;
        }

        /// <summary>
        /// Zipファイルの作成を開始します
        /// </summary>
        public void CreateZipArchive()
        {
            try
            {
                _entriesCount = 0;
                using (_archive = ZipFile.Open(_savePath, ZipArchiveMode.Create))
                {
                    CreateEntries();
                }
                ContinueWith.Invoke(null);
            }
            catch (Exception ex)
            {
                ContinueWith.Invoke(ex);
            }
        }

        /// <summary>
        /// List<FileSystemInfo>を元にZipアーカイブにエントリを追加する
        /// </summary>
        /// <exception cref="OperationCanceledException">CancellationTokenによってキャンセル要求があった場合中断する</exception>
        public void CreateEntries()
        {
            Exception exception = null;
            //ディレクトリのエントリ追加
            foreach (FileSystemInfo item in _targetItems)
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                try
                {
                    if (File.GetAttributes(item.FullName).HasFlag(FileAttributes.Directory))
                    {
                        CreateDirectoryEntry(item as DirectoryInfo);
                    }
                }
                catch (Exception ex)
                { 
                    exception = ex;
                }
            }

            foreach (FileSystemInfo item in _targetItems)
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                try
                {
                    if (!File.GetAttributes(item.FullName).HasFlag(FileAttributes.Directory))
                    {
                        CreateFileEntry(item as FileInfo);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            if (exception != null) { throw exception; }
        }
        private void CreateDirectoryEntry(DirectoryInfo file)
        {
            try
            {
                _archive.CreateEntry(GetRelativePath(_baseDir, file));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CreateFileEntry(FileInfo file)
        {
            try
            {
                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ZipArchiveEntry entry = _archive.CreateEntry(GetRelativePath(_baseDir, file));
                    using (var entryStream = entry.Open())
                    {
                        byte[] buffer = new byte[_bufferLength];

                        int read;

                        //CancellationTokenの確認 && StreamReaderがファイル末尾に到達していないかの確認
                        while ((_cToken.IsCancellationRequested == false) &&
                            (read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            entryStream.Write(buffer, 0, read);

                            //進捗状況の計算
                            _completedDataSize += read;
                            var p = (int)(100 * ((float)_completedDataSize / (float)_totalTargetDataSize));
                            if (p != _progress)
                            {
                                _progress = p;
                                OnProgressChanged(_progress);
                            }
                        }
                    }
                }
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{file.Name} :Open FileStream Failed {ex.ToString()}");
                throw ex;
            }
        }

        /// <summary>
        /// ディレクトリ内のファイルをZipアーカイブに追加し、子ディレクトリがあった場合、
        /// そのディレクトリを引数にして自身を再帰呼び出しする
        /// </summary>
        /// <param name="targetDir">探索するディレクトリ</param>
        /// <exception cref="OperationCanceledException">CancellationTokenによってキャンセル要求があった場合中断する</exception>
        public void CreateEntryRecurse(DirectoryInfo dir)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    _entriesCount++;

                    using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ZipArchiveEntry entry = _archive.CreateEntry(GetRelativePath(_baseDir, file));
                        using (var entryStream = entry.Open())
                        {
                            byte[] buffer = new byte[_bufferLength];

                            int read;

                            //CancellationTokenの確認 && StreamReaderがファイル末尾に到達していないかの確認
                            while ((_cToken.IsCancellationRequested == false) &&
                                (read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                entryStream.Write(buffer, 0, read);

                                //進捗状況の計算
                                _completedDataSize += read;
                                var p = (int)(100 * ((float)_completedDataSize / (float)_totalTargetDataSize));
                                if (p != _progress)
                                {
                                    _progress = p;
                                    OnProgressChanged(_progress);
                                }
                            }
                        }
                    }
                    if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{file.Name} :Open FileStream Failed {ex.ToString()}");
                    throw ex;
                }
            }

            //サブディレクトリを探索して、再帰呼び出し
            foreach (DirectoryInfo childDir in dir.GetDirectories())
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }

                try
                {
                    _archive.CreateEntry(GetRelativePath(_baseDir, childDir));
                    CreateEntryRecurse(childDir);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private void OnProgressChanged(int progress)
        {
            ProgressChanged?.BeginInvoke(this, new ProgressChangedEventArgs(progress), iar =>
            {
                try
                {
                    ProgressChanged.EndInvoke(iar);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    //StatusHelper.UpdateStatus(ex.Message);
                }
            }, null);
        }

        private static string GetRelativePath(FileSystemInfo relativeTo, FileSystemInfo target)
        {
            string basePath = GetFullNameWithDirectorySeparator(relativeTo);
            string fullPath = GetFullNameWithDirectorySeparator(target);

            if (fullPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase) == 0)
                return fullPath.Remove(0, basePath.Length);
            else
                return String.Empty;
        }

        private static string GetFullNameWithDirectorySeparator(FileSystemInfo fi)
        {
            var path = fi.FullName;
            if (fi.Attributes.HasFlag(FileAttributes.Directory) &&
                path.LastOrDefault() != Path.DirectorySeparatorChar
            )
                return path + Path.DirectorySeparatorChar;
            else
                return path;
        }
    }
}
