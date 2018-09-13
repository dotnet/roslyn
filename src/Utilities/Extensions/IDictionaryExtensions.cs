using System.Collections.Generic;
using System.Diagnostics;

namespace Analyzer.Utilities.Extensions
{
    internal static class IDictionaryExtensions
    {
        public static void AddKeyValueIfNotNull<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
        {
            if (key != null && value != null)
            {
                dictionary.Add(key, value);
            }
        }

        public static void AddRange<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                dictionary.Add(item);
            }
        }
    }
}
