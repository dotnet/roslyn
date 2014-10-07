using System;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    internal class ConditionalWeakTriple<TKey1, TKey2, TValue>
        where TKey1 : class
        where TKey2 : class
        where TValue : class
    {
        private readonly ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, TValue>> weakTable =
            new ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, TValue>>();

        private static readonly ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, TValue>>.CreateValueCallback callback =
            _ => new ConditionalWeakTable<TKey2, TValue>();

        public ConditionalWeakTriple()
        {
        }

        public TValue GetValue(TKey1 key1, TKey2 key2, Func<TKey1, TKey2, TValue> create)
        {
            // avoid creation of captured state if we can.
            TValue value;
            if (this.TryGetValue(key1, key2, out value))
            {
                return value;
            }
            else
            {
                return this.weakTable.GetValue(key1, callback).GetValue(key2, _ => create(key1, key2));
            }
        }

        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
        {
            value = default(TValue);
            ConditionalWeakTable<TKey2, TValue> innerTable;
            return
                this.weakTable.TryGetValue(key1, out innerTable) &&
                innerTable.TryGetValue(key2, out value);
        }
    }
}