using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBackup.Extensions
{
    internal static class FileSystemInfoExtensions
    {
        public static string GetRelativePath(this FileSystemInfo target, FileSystemInfo relativeTo)
        {
            string basePath = GetFullNameWithDirectorySeparator(relativeTo);
            string fullPath = GetFullNameWithDirectorySeparator(target);

            if (fullPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase) == 0)
                return fullPath.Remove(0, basePath.Length);
            else
                return String.Empty;
        }

        public static string GetFullNameWithDirectorySeparator(FileSystemInfo fi)
        {
            var path = fi.FullName;
            if (fi.Attributes.HasFlag(FileAttributes.Directory) &&
                path.LastOrDefault() != Path.DirectorySeparatorChar
            )
                return path + Path.DirectorySeparatorChar;
            else
                return path;
        }

        public static string WithDirectorySeparator(string path)
        {
            if (path.LastOrDefault() != Path.DirectorySeparatorChar)
                return path + Path.DirectorySeparatorChar;
            else
                return path;
        }
    }
}
