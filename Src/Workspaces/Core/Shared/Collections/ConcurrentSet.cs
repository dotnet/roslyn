using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Roslyn.Services.Shared.Collections
{
    internal class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, T> dictionary;

        public ConcurrentSet()
        {
            dictionary = new ConcurrentDictionary<T, T>();
        }

        public ConcurrentSet(IEqualityComparer<T> equalityComparer)
        {
            dictionary = new ConcurrentDictionary<T, T>(equalityComparer);
        }

        public bool Contains(T value)
        {
            return dictionary.ContainsKey(value);
        }

        public bool Add(T value)
        {
            return dictionary.TryAdd(value, value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
