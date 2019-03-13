using System;

namespace Roslyn.Utilities
{
    interface ICacheEntry<out TKey, out TValue>
    {
        TKey Key { get; }
        TValue Value { get; }
    }
}
