using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleBackup
{
    internal class DirectoryCleaning
    {
        private readonly StringIEqualityComparer comparer = new StringIEqualityComparer();

        public static void DeleteAllExcept(string path, string exclude)
        {
            DeleteAllExcept(path, new string[] { exclude });
        }

        public static void DeleteAllExcept(string path, string[] exclude)
        {
            var i = new DirectoryCleaning();
            var directories = Directory.GetDirectories(path);
            foreach (var di in directories)
            {
                i.DeleteAllExceptCore(di, exclude);
            }
        }

        private void DeleteAllExceptCore(string path, string[] exclude)
        {
            var files = Directory.GetFiles(path);
            foreach (var fi in files)
            {
                if (!exclude.Contains(fi, comparer))
                {
                    File.Delete(fi);
                }
            }

            var directories = Directory.GetDirectories(path);
            foreach (var di in directories)
            {
                DeleteAllExceptCore(di, exclude);
            }

            try
            {
                Directory.Delete(path);
            }
            catch { }
        }
    }

    public class StringIEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}
