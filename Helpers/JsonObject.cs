using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBackup.Helpers
{
    internal class JsonObject
    {
        public List<BackupTask> BackupHistory;
        public List<string> IgnoreItems;

        [JsonIgnore]
        public string FileName = "backup_history.json";
        //StreamWriterの順番待ち用
        private readonly object _writerLockObject = new Object();

        public JsonObject() { }

        /// <summary>
        /// オブジェクトをJson形式で保存します
        /// </summary>
        /// <param name="destinationPath">保存先</param>
        public Task SerializeToFileAsync(string destinationPath)
        {
            if (!Directory.Exists(destinationPath)) { return null; }

            var path = Path.Combine(destinationPath, FileName);
            try
            {
                return Task.Run(() =>
                {
                    lock (_writerLockObject)
                    {
                        using (var writer = new StreamWriter(path))
                        {
                            writer.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Jsonファイルからオブジェクトを復元します
        /// </summary>
        /// <param name="directory">Jsonファイルが存在するディレクトリ</param>
        /// <returns></returns>
        public Task DeserializeFromFileAsync(string directory)
        {
            var path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) { return Task.FromException(new FileNotFoundException()); }

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Open))
                        using (var sr = new StreamReader(stream))
                        {
                            var obj = JsonConvert.DeserializeObject<JsonObject>(sr.ReadToEnd());
                            this.BackupHistory = obj.BackupHistory;
                            this.IgnoreItems = obj.IgnoreItems;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return Task.FromException(new IOException());
            }
        }
    }
}
