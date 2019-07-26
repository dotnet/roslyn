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
    }
}
