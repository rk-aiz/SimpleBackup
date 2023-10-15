using SimpleBackup.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SimpleBackup.Controls
{
    /// <summary>
    /// StringFormatをプロパティとして公開することでバインディングターゲットに設定できる
    /// (StringFormatのローカライズ切り替え用)
    /// </summary>
    internal class FormattedTextBlock : TextBlock
    {
        public string TextStringFormat
        {
            get { return (string)GetValue(TextStringFormatProperty); }
            set { SetValue(TextStringFormatProperty, value); }
        }

        public static readonly DependencyProperty TextStringFormatProperty =
            DependencyProperty.Register(
                "TextStringFormat",
                typeof(string),
                typeof(FormattedTextBlock),
                new PropertyMetadata("{0}", (sender, e) =>
                {
                    Binding oldBinding = BindingOperations.GetBinding(sender, TextBlock.TextProperty);
                    CultureAwareBinding newBinding = oldBinding.CreateCopy<CultureAwareBinding>((string)e.NewValue);
                    BindingOperations.SetBinding(sender, TextBlock.TextProperty, newBinding);
                })
            );
    }
}
