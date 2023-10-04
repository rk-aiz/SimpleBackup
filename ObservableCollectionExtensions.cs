using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SimpleBackup
{
    internal static class ObservableCollectionExtensions
    {
        internal static void AddRange<T>(this ObservableCollection<T> source, IEnumerable<T> collection)
        {
            var itProperty = typeof(ObservableCollection<T>).GetProperty("Items", BindingFlags.NonPublic | BindingFlags.Instance);
            var colResetMethod = typeof(ObservableCollection<T>).GetMethod("OnCollectionReset", BindingFlags.NonPublic | BindingFlags.Instance);

            if (itProperty.GetValue(source) is List<T> list)
            {
                list.AddRange(collection);
                colResetMethod.Invoke(source, null);
            }
        }
    }
}
