using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Data;
using SimpleBackup.Extensions;

namespace SimpleBackup
{
    public class FileSystemTreeNode : INotifyPropertyChanged
    {
        private bool _IsExpanded = false;
        private bool? _IsChecked = true;
        private string _Name = "";
        private FileSystemTreeNode _Parent = null;
        private ObservableCollection<FileSystemTreeNode> _Children = null;
        private FileAttributes _Attributes = FileAttributes.Normal;

        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set {
                if (value == true )
                    if (CheckHaveDummy())
                    {
                        GetChildren();
                    }
                _IsExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        public bool? IsChecked
        {
            get { return _IsChecked; }
            set
            { _IsChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged("Text"); }
        }

        public FileAttributes Attributes
        {
            get { return _Attributes; }
            set { _Attributes = value; OnPropertyChanged("Attrubutes"); }
        }

        public FileSystemTreeNode Parent
        {
            get { return _Parent; }
            set { _Parent = value; OnPropertyChanged("Parent"); }
        }

        public ObservableCollection<FileSystemTreeNode> Children { get; private set; } = new ObservableCollection<FileSystemTreeNode>();

        private object __lockObj = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        public FileSystemTreeNode()
        {

        }

        private void OnPropertyChanged(string name)
        {
            if (null == this.PropertyChanged) return;
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public void Add(FileSystemTreeNode child)
        {
            if (null == Children)
            {
                Children = new ObservableCollection<FileSystemTreeNode>();
                //BindingOperations.EnableCollectionSynchronization(Children, __lockObj);
            }
            child.Parent = this;
            Children.Add(child);
        }

        public void Clear()
        {
            Children?.Clear();
        }

        //TreeのIsCheckedを親方向へ伝搬
        public void UpdateParentStatus()
        {
            if (null != Parent)
            {
                int isCheckedNull = 0;
                int isCheckedOn = 0;
                int isCheckedOff = 0;
                if (null != Parent.Children)
                {
                    foreach (var item in Parent.Children)
                    {
                        if (null == item.IsChecked) isCheckedNull += 1;
                        if (true == item.IsChecked) isCheckedOn += 1;
                        if (false == item.IsChecked) isCheckedOff += 1;
                    }
                }
                if ((0 < isCheckedNull) || (0 < isCheckedOn) || (0 < isCheckedOff))
                {
                    if (0 < isCheckedNull)
                        Parent.IsChecked = null;
                    else if ((0 < isCheckedOn) && (0 < isCheckedOff))
                        Parent.IsChecked = null;
                    else if (0 < isCheckedOn)
                        Parent.IsChecked = true;
                    else
                        Parent.IsChecked = false;
                }
                Parent.UpdateParentStatus();
            }
        }

        //TreeのIsCheckedを子方向へ伝搬
        public void UpdateChildStatus()
        {
            if (null != IsChecked)
            {
                if (null != Children)
                {
                    foreach (var item in Children)
                    {
                        item.IsChecked = IsChecked;
                        item.UpdateChildStatus();
                    }
                }
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

            if (withDirectorySeparatorChar &&
                File.GetAttributes(path).HasFlag(FileAttributes.Directory) &&
                Name.LastOrDefault() != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar.ToString();
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// 子ノードの取得
        /// </summary>
        /// <param name="di"></param>
        public void GetChildren(DirectoryInfo di = null)
        {
            if (di == null) { di = new DirectoryInfo(GetPath()); }
            
            if (!di.Exists) { return; }
            var list = new List<FileSystemTreeNode>();

            //ファイル項目取得
            foreach (FileInfo fi in di.EnumerateFiles("*"))
            {
                try
                {
                    list.Add(new FileSystemTreeNode()
                    {
                        Name = fi.Name,
                        Attributes = File.GetAttributes(fi.FullName),
                        IsChecked = this.IsChecked,
                        Parent = this
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            //ディレクトリ項目取得
            foreach (DirectoryInfo child in di.EnumerateDirectories("*"))
            {
                try
                {
                    var node = new FileSystemTreeNode()
                    {
                        Name = child.Name,
                        Attributes = File.GetAttributes(child.FullName),
                        IsChecked = this.IsChecked,
                        Parent = this
                    };
                    //子にダミーを追加してExpand可能にする
                    node.Add(new DummyFileSystemTreeNode());
                    list.Add(node);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            if (list.Count > 0)
            {
                Clear();
                if (Children == null) { Children = new ObservableCollection<FileSystemTreeNode>(); }
                Children.AddRange(list);
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
        /// <returns></returns>
        public List<FileSystemInfo> GetCheckedItems()
        {
            return GetCheckedItemsCore(this);
        }

        public List<FileSystemInfo> GetCheckedItemsCore(FileSystemTreeNode node)
        {
            if (node.CheckHaveDummy())
            {
                node.GetChildren(new DirectoryInfo(node.GetPath()));
            }
            if (node.Children == null || node.Children.Count == 0) { return null; }

            List<FileSystemInfo> items = new List<FileSystemInfo>();

            //「チェックされている」項目を抜粋
            var checkedNodes = node.Children.Where(item => (item.IsChecked != false));

            //「ファイル」項目を追加
            items.AddRange(checkedNodes.Where(item  => !File.GetAttributes(item.GetPath()).HasFlag(FileAttributes.Directory))
                                       .Select(item => new FileInfo(item.GetPath())));

            //「ディレクトリ」項目を追加、再帰探査
            foreach (var dirNode in checkedNodes.Where(item => File.GetAttributes(item.GetPath()).HasFlag(FileAttributes.Directory)))
            {
                items.Add(new DirectoryInfo(dirNode.GetPath()));
                if (GetCheckedItemsCore(dirNode) is List<FileSystemInfo> childItems)
                {
                    items.AddRange(childItems);
                }
            }

            return items;
        }
        /// <summary>
        /// 絶対パスを指定して、指定項目をIsChecked = falseにします
        /// </summary>
        /// <param name="ignoreItems"></param>
        public void SetIgnoreItems(IEnumerable<string> ignoreItems)
        {
            if (ignoreItems == null) { return; }
            foreach (var item in ignoreItems)
            {
                try
                {
                    TrySetIgnore(item, false);
                }
                catch { }
            }
        }

        public bool TrySetIgnore(string path, bool value)
        {
            if (path.IndexOf(GetPath(), StringComparison.OrdinalIgnoreCase) == 0)
            {
                string relativePath;
                try
                {
                    relativePath = path.Remove(0, GetPath(true).Length);
                }
                catch
                { 
                    return false;
                }
                if (relativePath == Path.DirectorySeparatorChar.ToString() ||
                    String.IsNullOrEmpty(relativePath))
                {
                    IsChecked = value;
                    UpdateParentStatus();
                    UpdateChildStatus();
                    return true;
                }
                else
                {
                    if (relativePath.Split(Path.DirectorySeparatorChar).First() is string nextNode)
                    {
                        if (CheckHaveDummy()) { GetChildren(new DirectoryInfo(GetPath())); }
                        return Children?.Where(node => node.Name == nextNode).First()?.TrySetIgnore(path, value) ?? false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
                return false;
        }

        public bool CheckHaveDummy()
        {
            if (Children?.FirstOrDefault() is DummyFileSystemTreeNode)
                return true;
            else
                return false;
        }
    }

    class DummyFileSystemTreeNode : FileSystemTreeNode
    {
        public DummyFileSystemTreeNode()
        {

        }
    }
}
