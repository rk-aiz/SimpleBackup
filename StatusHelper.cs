using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace SimpleBackup
{
    public sealed class StatusHelper : INotifyPropertyChanged
    {
        private static StatusHelper _singleton = new StatusHelper();

        public static bool UpdateEnabled;

        public static Storyboard ProgressAnimation;

        private Visibility _progressRingVisibility = Visibility.Collapsed;
        public Visibility ProgressRingVisibility
        {
            get { return _progressRingVisibility; }
            set { _progressRingVisibility = value; OnPropertyChanged("ProgressRingVisibility"); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private StatusHelper()
        {
            UpdateEnabled = false;
        }

        public static StatusHelper GetInstance()
        {
            return _singleton;
        }

        public static StatusHelper Instance
        {
            get { return _singleton; }
        }

        public static void UpdateStatus(string text)
        {
            if (UpdateEnabled == true)
            {
                StatusHelper shi = GetInstance();
                shi.Message = text;
            }
        }

        public static void ShowProgressRing()
        {
            if (UpdateEnabled == true)
            {
                StatusHelper shi = GetInstance();
                shi.ProgressRingVisibility = Visibility.Visible;
                ProgressAnimation?.Begin();
            }
        }

        public static void HideProgressRing()
        {

            if (UpdateEnabled == true)
            {
                StatusHelper shi = GetInstance();
                shi.ProgressRingVisibility = Visibility.Collapsed;
                ProgressAnimation?.Stop();
            }
        }

    }
}
