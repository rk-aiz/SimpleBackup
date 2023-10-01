using System;
using System.ComponentModel;

namespace SimpleBackup
{
    /// <summary>
    /// バックアップ実施履歴を表すクラス
    /// </summary>
    internal class BackupHistoryEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isSequence = true;
        public bool IsSequence
        {
            get { return _isSequence; }
            set { _isSequence = value; OnPropertyChanged("IsSequence"); }
        }

        public string SourcePath { get; set; }

        private string _saveDir;
        public string SaveDir
        {
            get { return _saveDir; }
            set { _saveDir = value; OnPropertyChanged("SaveDir"); }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; OnPropertyChanged("FileName"); }
        }

        private DateTime _saveTime;
        public DateTime SaveTime
        {
            get { return _saveTime; }
            set { _saveTime = value; OnPropertyChanged("SaveTime"); }
        }

        private int _index;
        public int Index
        {
            get { return _index; }
            set { _index = value; OnPropertyChanged("Index"); }
        }

        public BackupHistoryEntry()
        {
            _saveTime = DateTime.Now;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
