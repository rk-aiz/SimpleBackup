using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SimpleBackup.Helpers
{
    internal static class JsonHelper
    {
        public static string FileName = "backup_history.json";
        //StreamWriterの順番待ち用
        private static readonly object _writerLockObject = new Object();

        /// <summary>
        /// オブジェクトをJson形式で保存します
        /// </summary>
        /// <param name="saveObject">保存するオブジェクト</param>
        /// <param name="destinationPath">保存先</param>
        public static async void SerializeToFile(object saveObject, string destinationPath)
        {
            if (!Directory.Exists(destinationPath)) { return; }

            var path = Path.Combine(destinationPath, FileName);
            try
            {
                await Task.Run(() =>
                {
                    lock (_writerLockObject)
                    {
                        using (var writer = new StreamWriter(path))
                        {
                            writer.WriteLine(JsonConvert.SerializeObject(saveObject, Formatting.Indented));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Jsonファイルからオブジェクトを復元します
        /// </summary>
        /// <typeparam name="T">復元するオブジェクトの型</typeparam>
        /// <param name="directory">Jsonファイルが存在するディレクトリ</param>
        /// <returns></returns>
        public static async Task<T> DeserializeFromFile<T>(string directory)
        {
            var path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) { return default; }

            try
            {
                return await Task.Run(() =>
                {
                    using (var stream = new FileStream(path, FileMode.Open))
                    using (var sr = new StreamReader(stream))
                    {
                        return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return default;
            }
        }
    }
}
