using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;

namespace SimpleBackup
{
    public static class ZipArchiveHelper
    {
        /// <summary>
        /// ディレクトリ配下のファイルとディレクトリを順次アーカイブに追加します。
        /// </summary>
        public static void CreateZipArchive(this DirectoryInfo di, string savePath)
        {
            try
            {
                if (System.IO.File.Exists(savePath))
                {
                    System.IO.File.Delete(savePath);
                }

                using (ZipArchive archive = ZipFile.Open(savePath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryRecurse(di, di);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
        }

        private static void CreateEntryRecurse(this ZipArchive archive, DirectoryInfo baseDir, DirectoryInfo targetDir)
        {
            foreach (DirectoryInfo subDir in targetDir.GetDirectories())
            {
                try
                {
                    archive.CreateEntry(GetRelativePath(baseDir, subDir));
                    archive.CreateEntryRecurse(baseDir, subDir);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            foreach (FileInfo file in targetDir.GetFiles())
            {
                try
                {
                    using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ZipArchiveEntry entry = archive.CreateEntry(GetRelativePath(baseDir, file));
                        using (var entryStream = entry.Open()) { fs.CopyTo(entryStream); }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
        static public string GetRelativePath(FileSystemInfo relativeTo, FileSystemInfo target)
        {
            string basePath = GetFullNameWithDirectorySeparator(relativeTo);
            string fullPath = GetFullNameWithDirectorySeparator(target);

            if (fullPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase) == 0)
                return fullPath.Remove(0, basePath.Length);
            else
                return String.Empty;
        }

        static private string GetFullNameWithDirectorySeparator(FileSystemInfo fi)
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
