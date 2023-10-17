using SimpleBackup.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;

namespace SimpleBackup
{
    public class FileSystemTreeNode : INotifyPropertyChanged
    {
        private bool _IsExpanded = false;
        private bool? _IsChecked = true;
        private string _Name = string.Empty;
        private long _length = -1;
        private FileSystemTreeNode _Parent = null;
        private bool _isDirectory;
        public bool NotDummy { get; set; } = true;

        public bool IsDirectory { get { return _isDirectory; } }

        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                if (value == true)
                    if (CheckHaveDummy())
                    {
                        GetChildrenAsync();
                    }
                _IsExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        public bool? IsChecked
        {
            get { return _IsChecked; }
            set
            {
                _IsChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged("Text"); }
        }

        public long Length
        {
            get { return _length; }
            set { _length = value; OnPropertyChanged("Length"); }
        }

        public FileSystemTreeNode Parent
        {
            get { return _Parent; }
            set { _Parent = value; OnPropertyChanged("Parent"); }
        }

        private object _lockObject = new object();
        public ObservableCollection<FileSystemTreeNode> Children { get; } = new ObservableCollection<FileSystemTreeNode>();
        private Dispatcher _dp;

        public event PropertyChangedEventHandler PropertyChanged;

        public FileSystemTreeNode(Dispatcher dp, bool isDir)
        {
            _dp = dp;
            _isDirectory = isDir;
            BindingOperations.EnableCollectionSynchronization(Children, _lockObject);
        }

        private void OnPropertyChanged(string name)
        {
            if (null == this.PropertyChanged) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public void Add(FileSystemTreeNode child)
        {
            child.Parent = this;
            lock (_lockObject)
            {
                Children.Add(child);
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                Children?.Clear();
            }
        }

        /// <summary>
        /// TreeのIsCheckedを親方向へ伝搬
        /// </summary>
        public void UpdateParentStatus()
        {
            if (Parent == null || Parent.Children == null) { return; }

            var e = Parent.Children.Select(node => node.IsChecked);

            if (e.Contains(null)) { Parent.IsChecked = null; }
            else if (e.Contains(true))
            {
                if (e.Contains(false)) { Parent.IsChecked = null; }
                else { Parent.IsChecked = true; }
            }
            else { Parent.IsChecked = false; }
            Parent.UpdateParentStatus();
        }

        /// <summary>
        /// TreeのIsCheckedを子方向へ伝搬
        /// </summary>
        public void UpdateChildStatus()
        {
            if (null == IsChecked || null == Children) { return; }

            foreach (var item in Children)
            {
                item.IsChecked = IsChecked;
                item.UpdateChildStatus();
            }

        }

        /// <summary>
        /// フルパスを解決
        /// </summary>
        /// <param name="withDirectorySeparatorChar">ディレクトリパスを区切り文字付きで返します</param>
        /// <returns></returns>
        public string GetPath(bool withDirectorySeparatorChar = false)
        {
            string path;
            if (Parent == null)
                path = Name;
            else
                path = Path.Combine(Parent.GetPath(), Name);

            if (withDirectorySeparatorChar)
            {
                if (IsDirectory && Name.LastOrDefault() != Path.DirectorySeparatorChar)
                {
                    return path + Path.DirectorySeparatorChar.ToString();
                }
            }
            return path;
        }

        /// <summary>
        /// 子ノードの取得
        /// </summary>
        /// <param name="di"></param>
        public DispatcherOperation GetChildrenAsync(DirectoryInfo di = null)
        {
            return _dp.InvokeAsync(() =>
            {
                GetChildren(di);
            });
        }

        public void GetChildren(DirectoryInfo di = null)
        {
            if (di == null) { di = new DirectoryInfo(GetPath()); }
            if (!di.Exists) { return; }

            var list = new List<FileSystemTreeNode>();
            try
            {
                //ファイル項目取得
                foreach (FileInfo fi in di.EnumerateFiles("*"))
                {
                    list.Add(new FileSystemTreeNode(_dp, false)
                    {
                        Name = fi.Name,
                        Length = fi.Length,
                        IsChecked = this.IsChecked,
                        Parent = this
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            //ディレクトリ項目取得
            try
            {
                foreach (DirectoryInfo child in di.EnumerateDirectories("*"))
                {
                    var node = new FileSystemTreeNode(_dp, true)
                    {
                        Name = child.Name,
                        IsChecked = this.IsChecked,
                        Parent = this
                    };
                    //子にダミーを追加してExpand可能にする
                    node.Add(new FileSystemTreeNode(_dp, true) { NotDummy = false });
                    list.Add(node);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (list.Count > 0)
            {
                Clear();
                lock (_lockObject)
                {
                    Children.AddRange(list);
                }
            }
        }

        /// <summary>
        /// IsChecked == falseのアイテムを取得します
        /// </summary>
        /// <returns></returns>
        public List<string> GetIgnoreItems()
        {
            return GetIgnoreItemsCore(this);
        }

        public List<string> GetIgnoreItemsCore(FileSystemTreeNode node)
        {
            if (CheckHaveDummy())
            {
                node.GetChildren(new DirectoryInfo(GetPath()));
            }
            if (node.Children == null || node.Children.Count == 0) { return null; }

            List<string> items = new List<string>();

            //「チェックされていない」項目を抜粋
            items.AddRange(node.Children.Where(item => item.IsChecked == false)
                         .Select(item => item.GetPath(true)));

            //「不明(not sure)」項目を再帰探査
            foreach (var dirNode in node.Children.Where(item => item.IsChecked == null))
            {
                if (GetIgnoreItemsCore(dirNode) is List<string> childItems)
                {
                    items.AddRange(childItems);
                }
            }
            return items;
        }

        /// <summary>
        /// IsChecked == true, nullのアイテムを取得します
        /// </summary>
        public Task<FileSystemList> GetCheckedItemsAsync()
        {
            return Task.Run(() => GetCheckedItemsCore(this));
        }

        private int _maxThread = 16;
        private int _thread = 0;

        public FileSystemList GetCheckedItemsCore(FileSystemTreeNode node)
        {
            if (node.CheckHaveDummy())
            {
                node.GetChildren(new DirectoryInfo(node.GetPath()));
            }
            if (node.Children == null || node.Children.Count == 0) { return null; }

            FileSystemList fsl = new FileSystemList();

            //「チェックされている」項目を抜粋
            var checkedNodes = node.Children.Where(item => (item.IsChecked != false));

            var fileNodes = checkedNodes.Where(item => File.Exists(item.GetPath()));
            fsl.AddFileNodes(fileNodes);

            var dirNodes = checkedNodes.Where(item => Directory.Exists(item.GetPath()));
            fsl.AddDirectoryNodes(dirNodes);

            //「ディレクトリ」項目を再帰探査

            if (checkedNodes.Take(2)?.Count() > 1 && _thread <= _maxThread)
            {
                Parallel.ForEach(dirNodes, dir =>
                {
                    Interlocked.Increment(ref _thread);
                    if (GetCheckedItemsCore(dir) is FileSystemList childList)
                    {
                        fsl.Add(childList);
                    }
                });
            }
            else
            {
                foreach (var dirNode in checkedNodes.Where(item => item.IsDirectory))
                {
                    if (GetCheckedItemsCore(dirNode) is FileSystemList childList)
                    {
                        fsl.Add(childList);
                    }
                }
            }
            return fsl;
        }
        /// <summary>
        /// 絶対パス(の列挙)を指定して、指定項目をIsChecked = falseにする
        /// </summary>
        public void SetIgnoreItems(IEnumerable<string> ignoreItems)
        {
            if (ignoreItems == null) { return; }
            foreach (var item in ignoreItems)
            {
                try
                {
                    TrySetIgnore(this, item, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        public static void TrySetIgnore(FileSystemTreeNode node, string path, bool value)
        {
            //pathがただしいパスであるかの確認
            if (path.IndexOf(node.GetPath(), StringComparison.OrdinalIgnoreCase) == 0)
            {
                string relativePath = path.Remove(0, node.GetPath(true).Length);

                //pathがこのNodeを示している場合IsCheckedに代入
                if (relativePath == Path.DirectorySeparatorChar.ToString() ||
                    String.IsNullOrEmpty(relativePath))
                {
                    node.IsChecked = value;
                    node.UpdateChildStatus();
                    node.UpdateParentStatus();
                    return;
                }
                else
                {
                    if (relativePath.Split(Path.DirectorySeparatorChar).First() is string nextNode)
                    {
                        if (node.CheckHaveDummy()) { node.GetChildren(new DirectoryInfo(node.GetPath())); }
                        foreach (var child in node.Children)
                        {
                            if (child.Name == nextNode)
                            {
                                TrySetIgnore(child, path, value);
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        public bool CheckHaveDummy()
        {
            if (Children?.FirstOrDefault()?.NotDummy == false)
                return true;
            else
                return false;
        }

        public void CheckAll()
        {
            foreach (var child in Children)
            {
                child.IsChecked = true;
                child.UpdateChildStatus();
            }
        }

        public void UncheckAll()
        {
            foreach (var child in Children)
            {
                child.IsChecked = false;
                child.UpdateChildStatus();
            }
        }
    }
}
