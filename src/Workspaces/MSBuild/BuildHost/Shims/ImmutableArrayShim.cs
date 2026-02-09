// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Minimal shim for ImmutableArray<T> on net472, where System.Collections.Immutable
// is no longer referenced.  Only the surface consumed by PathUtilities.NormalizePathPrefix
// is implemented.

#if !NET

using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal readonly struct ImmutableArray<T> : IEnumerable<T>
    {
#pragma warning disable CS0649 // Field is never assigned to
        private readonly T[]? _items;
#pragma warning restore CS0649

        public bool IsDefaultOrEmpty => _items is null || _items.Length == 0;

        public int Length => _items?.Length ?? 0;

        public T this[int index] => (_items ?? Array.Empty<T>())[index];

        public IEnumerator<T> GetEnumerator()
            => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // These operators exist solely to satisfy the null-check pattern used in Hash.cs
        // (e.g. `if (values == null)`). This shim is never instantiated by BuildHost code.
        public static bool operator ==(ImmutableArray<T> left, object? right)
            => right is null && left._items is null;

        public static bool operator !=(ImmutableArray<T> left, object? right)
            => !(left == right);

        public override bool Equals(object? obj) => obj is null && _items is null;

        public override int GetHashCode() => _items?.GetHashCode() ?? 0;
    }

    // Compilation-only shim; the ImmutableDictionary<TKey, TValue> type is referenced
    // by Hash.CombineValues but never called from BuildHost code paths.
    internal class ImmutableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

#endif
