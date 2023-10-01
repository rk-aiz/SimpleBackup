using System;
using System.ComponentModel;

namespace SimpleBackup
{
    /// <summary>
    /// ステータスバーに表示する情報を扱うクラス
    /// シングルトン
    /// インスタンス取得 -> Instance, GetInstance()
    /// </summary>
    public sealed class StatusHelper : INotifyPropertyChanged
    {
        private readonly static StatusHelper _current = new StatusHelper();

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //ステータスバー更新の有効/無効
        public static bool UpdateEnabled = false;

        //時間のかかる処理が実行中かどうかのフラグ
        private bool _progressStatus = false;
        public bool ProgressStatus
        {
            get { return _progressStatus; }
            set { _progressStatus = value; OnPropertyChanged("ProgressStatus"); }
        }

        //ステータスバーに表示するメッセージ
        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
        }

        public static StatusHelper GetInstance()
        {
            return _current;
        }

        public static StatusHelper Instance
        {
            get { return _current; }
        }

        public static void UpdateStatus(string text)
        {
            if (UpdateEnabled == true)
            {
                StatusHelper shi = GetInstance();
                shi.Message = text;
            }
        }

        public static void SetProgressStatus(bool status)
        {
            if (UpdateEnabled == true)
            {
                StatusHelper shi = GetInstance();
                shi.ProgressStatus = status;
            }
        }
    }
}
