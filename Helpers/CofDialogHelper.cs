using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Threading;

namespace SimpleBackup.Helpers
{
    /// <summary>
    /// CommonOpenFileDialog用ヘルパークラス
    /// 主目的はUIスレッドが固まらないようにDispatcher.InvokeAsyncを利用すること
    /// </summary>
    internal static class CofDialogHelper
    {
        public static DispatcherOperation<string> ShowDialogAsync(Dispatcher dp, string initDir, string title = "Select Folder")
        {
            return dp.InvokeAsync(() => ShowDialog(initDir, title));
        }

        private static string ShowDialog(string initDir, string title)
        {
            using (var cofDialog = new CommonOpenFileDialog()
            {
                Title = title,
                IsFolderPicker = true,
                InitialDirectory = initDir
            })
            {
                if (cofDialog.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return initDir;
                }
                else
                {
                    return cofDialog.FileName;
                }
            }
        }
    }
}
