using SimpleBackup.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

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
        private long _completedLength;

        private int _bufferLength;

        private string _savePath;
        private FileSystemNode _baseDir;
        private FileSystemList _targetList;

        private CancellationToken _cToken;

        /// <summary>
        /// Method that receives callbacks from threads upon completion.
        /// </summary>
        public delegate void ZipArchiveCompletedCallback(Exception ex);
        public ZipArchiveCompletedCallback ContinueWith { get; set; }

        /// <summary>
        /// ディレクトリからZipファイルを作成します
        /// </summary>
        /// <param name="baseDir">ソースとなるディレクトリ</param>
        /// <param name="savePath">保存先</param>
        /// <param name="token">CancellationToken</param>
        public ZipArchiveHelper(FileSystemNode baseDir, FileSystemList targetList, string savePath, CancellationToken token)
        {
            _savePath = savePath;
            _baseDir = baseDir;
            _cToken = token;
            _targetList = targetList;
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
            foreach (FileSystemNode item in _targetList.Items)
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                try
                {
                    if (item.IsDirectory)
                    {
                        CreateDirectoryEntry(item);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            foreach (FileSystemNode item in _targetList.Items)
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                try
                {
                    if (item.IsDirectory == false)
                    {
                        CreateFileEntry(item);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            if (exception != null) { throw exception; }
        }

        private void CreateDirectoryEntry(FileSystemNode directory)
        {
            try
            {
                _archive.CreateEntry(directory.GetRelativePath(_baseDir));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CreateFileEntry(FileSystemNode file)
        {
            try
            {
                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ZipArchiveEntry entry = _archive.CreateEntry(file.GetRelativePath(_baseDir));
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
                            _completedLength += read;
                            var p = (int)(100 * ((float)_completedLength / (float)_targetList.TotalLength));
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
                Debug.WriteLine($"{file.FullName} :Open FileStream Failed {ex.ToString()}");
                throw ex;
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
    }
}
