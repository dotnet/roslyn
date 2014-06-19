using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A concurrent, simplified HashSet.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, byte> data = new ConcurrentDictionary<T, byte>();

        public int Count
        {
            get
            {
                return data.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.Keys.GetEnumerator();
        }

        public bool Remove(T t)
        {
            byte b;
            return data.TryRemove(t, out b);
        }

        public bool Add(T t)
        {
            return data.TryAdd(t, 0);
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        public bool Contains(T t)
        {
            return data.ContainsKey(t);
        }
    }
}