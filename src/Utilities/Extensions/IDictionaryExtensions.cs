using System.Collections.Generic;

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
        public static void Reset<TKey, TValue>(
           this IDictionary<TKey, TValue> dictionary,
           IDictionary<TKey, TValue> newDictionaryOpt)
        {
            dictionary.Clear();
            if (newDictionaryOpt != null)
            {
                foreach (var kvp in newDictionaryOpt)
                {
                    dictionary.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
