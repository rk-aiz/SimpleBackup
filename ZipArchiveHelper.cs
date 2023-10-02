using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace SimpleBackup
{
    /// <summary>
    /// ディレクトリ配下のファイルとディレクトリを順次アーカイブに追加します。
    /// </summary>
    internal class ZipArchiveHelper
    {
        private ZipArchive _archive;
        public event ProgressChangedEventHandler ProgressChanged;

        private int _entriesCount;
        private int _totalTargetFiles;
        private long _totalTargetDataSize;
        private long _completedDataSize;

        private int _bufferLength;

        private string _savePath;
        private DirectoryInfo _baseDir;

        private CancellationToken _cToken;

        public ZipArchiveHelper(DirectoryInfo di, string savePath, CancellationToken token)
        {
            _savePath = savePath;
            _baseDir = di;
            _cToken = token;
            DirectoryMeasure dm = new DirectoryMeasure(_baseDir.FullName);
            _totalTargetFiles = dm.GetTotalCount();
            _totalTargetDataSize = dm.GetTotalSize();
            _bufferLength = 16 * 1024;
        }

        public void CreateZipArchive()
        {
            try
            {
                _entriesCount = 0;
                using (_archive = ZipFile.Open(_savePath, ZipArchiveMode.Create))
                {
                    CreateEntryRecurse(_baseDir);
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CreateEntryRecurse(DirectoryInfo targetDir)
        {
            foreach (DirectoryInfo subDir in targetDir.GetDirectories())
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }

                try
                {
                    _archive.CreateEntry(GetRelativePath(_baseDir, subDir));
                    CreateEntryRecurse(subDir);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            foreach (FileInfo file in targetDir.GetFiles())
            {
                if (_cToken.IsCancellationRequested) { throw new OperationCanceledException(); }

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
                            while ((_cToken.IsCancellationRequested == false) && (read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                entryStream.Write(buffer, 0, read);

                                _completedDataSize += read;
                                OnProgressChanged((int)(100 * ((float)_completedDataSize / (float)_totalTargetDataSize)));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(progress));
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
            if (fi.Attributes.HasFlag(FileAttributes.Directory) &&
                fi.FullName.LastOrDefault() != Path.DirectorySeparatorChar
            )
                return fi.FullName + Path.DirectorySeparatorChar;
            else
                return fi.FullName;
        }
    }
}
