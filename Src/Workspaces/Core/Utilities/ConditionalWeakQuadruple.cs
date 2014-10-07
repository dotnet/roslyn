using System;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    internal class ConditionalWeakQuadruple<TKey1, TKey2, TKey3, TValue>
        where TKey1 : class
        where TKey2 : class
        where TKey3 : class
        where TValue : class
    {
        private readonly ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>>> key1Table =
            new ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>>>();

        private static readonly ConditionalWeakTable<TKey1, ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>>>.CreateValueCallback createKey2Table =
            _ => new ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>>();

        private static readonly ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>>.CreateValueCallback createKey3Table =
            _ => new ConditionalWeakTable<TKey3, TValue>();

        public ConditionalWeakQuadruple()
        {
        }

        public TValue GetValue(TKey1 key1, TKey2 key2, TKey3 key3, Func<TKey1, TKey2, TKey3, TValue> create)
        {
            // avoid creation of captured state if we can.
            TValue value;
            if (this.TryGetValue(key1, key2, key3, out value))
            {
                return value;
            }
            else
            {
                return this.key1Table.GetValue(key1, createKey2Table)
                                     .GetValue(key2, createKey3Table)
                                     .GetValue(key3, _ => create(key1, key2, key3));
            }
        }

        public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value)
        {
            value = default(TValue);
            ConditionalWeakTable<TKey2, ConditionalWeakTable<TKey3, TValue>> key2Table;
            ConditionalWeakTable<TKey3, TValue> key3Table;

            return this.key1Table.TryGetValue(key1, out key2Table)
                && key2Table.TryGetValue(key2, out key3Table)
                && key3Table.TryGetValue(key3, out value);
        }
    }
}