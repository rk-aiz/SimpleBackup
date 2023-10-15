using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleBackup.Controls
{
    /// <summary>
    /// ディレクトリパス用のTextBox
    /// </summary>
    public partial class DirectoryPathTextBox : TextBox
    {
        /// <summary>
        /// TextBox入力内容の説明
        /// </summary>
        public string Description
        {
            get { return GetValue(DescriptionProperty) as string; }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DirectoryPathTextBox),
                                        new PropertyMetadata(String.Empty, (s, e) =>
        {   //DependencyPropertyChangedEvent
            if (!(s is DirectoryPathTextBox tb)) { return; }
            if (!(e.NewValue is string value)) { return; }

            //DescriptionをToolTipに反映
            if (!string.IsNullOrEmpty(value))
                ToolTipService.SetToolTip(s, value);
        }));

        /// <summary>
        /// Selectedルーティングイベント(クリックorフォーカス状態でEnterキーを押した)
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add { AddHandler(SelectedEvent, value); }
            remove { RemoveHandler(SelectedEvent, value); }
        }

        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent(
            name: "Selected",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(DirectoryPathTextBox));

        public DirectoryPathTextBox()
        {
            //Selectedイベントの発行の準備
            GotMouseCapture += (s, e) =>
            {
                RaiseEvent(new RoutedEventArgs(routedEvent: SelectedEvent));
            };
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    RaiseEvent(new RoutedEventArgs(routedEvent: SelectedEvent));
                }
            };
        }
    }
}
