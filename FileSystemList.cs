using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SimpleBackup
{
    public class FileSystemList
    {
        private object _lockObject = new object();
        private int _filescount = 0;
        private long _totalLength = 0;
        public List<FileSystemNode> Items { get; } = new List<FileSystemNode>();

        public int FilesCount { get { return _filescount; } }
        public long TotalLength { get { return _totalLength; } }
        public FileSystemList()
        {
        }

        public void Add(FileSystemList fl)
        {
            Interlocked.Add(ref _filescount, fl.FilesCount);
            Interlocked.Add(ref _totalLength, fl.TotalLength);
            lock (_lockObject)
            {
                Items.AddRange(fl.Items);
            }
        }

        public void AddTreeNodes(IEnumerable<FileSystemTreeNode> nodes)
        {
            int count = 0;
            long length = 0;
            lock (_lockObject)
            {
                Items.AddRange(nodes.Select(node =>
                {
                    if (!node.IsDirectory)
                    {
                        count++;
                        length += node.Length;
                    }
                    return new FileSystemNode(node.GetPath(true), node.IsDirectory);
                }));
            }
            Interlocked.Add(ref _filescount, count);
            Interlocked.Add(ref _totalLength, length);
        }

        public void AddFileNodes(IEnumerable<FileSystemTreeNode> nodes)
        {
            int count = 0;
            long length = 0;
            lock (_lockObject)
            {
                Items.AddRange(nodes.Select(node =>
                {
                    count++;
                    length += node.Length;
                    return new FileSystemNode(node.GetPath(true), false);
                }));
            }
            Interlocked.Add(ref _filescount, count);
            Interlocked.Add(ref _totalLength, length);
        }

        public void AddDirectoryNodes(IEnumerable<FileSystemTreeNode> nodes)
        {
            lock (_lockObject)
            {
                Items.AddRange(nodes.Select(node => new FileSystemNode(node.GetPath(true), true)));
            }
        }
    }

    public class FileSystemNode
    {
        public string FullName;
        public bool IsDirectory;

        public FileSystemNode(string fullName, bool isDirectory)
        {
            FullName = fullName;
            IsDirectory = isDirectory;
        }

        public string GetRelativePath(FileSystemNode relativeTo)
        {
            string fullPath = GetPathWithDirecrotySeparator();
            string relativePath = relativeTo.GetPathWithDirecrotySeparator();

            if (fullPath.IndexOf(relativePath, StringComparison.OrdinalIgnoreCase) == 0)
                return fullPath.Remove(0, relativePath.Length);
            else
                return string.Empty;
        }

        private string GetPathWithDirecrotySeparator()
        {
            return IsDirectory && FullName.LastOrDefault() != System.IO.Path.DirectorySeparatorChar ?
                FullName + System.IO.Path.DirectorySeparatorChar : FullName;
        }
    }
}
