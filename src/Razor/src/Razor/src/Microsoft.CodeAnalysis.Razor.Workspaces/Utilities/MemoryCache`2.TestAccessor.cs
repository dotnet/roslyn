// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal partial class MemoryCache<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    internal TestAccessor GetTestAccessor() => new(this);

    internal struct TestAccessor(MemoryCache<TKey, TValue> instance)
    {
        public event Action Compacted
        {
            add => instance._compactedHandler += value;
            remove => instance._compactedHandler -= value;
        }

        public readonly bool TryGetLastAccess(TKey key, out DateTime result)
        {
            if (instance._map.TryGetValue(key, out var value))
            {
                result = value.LastAccess;
                return true;
            }

            result = default;
            return false;
        }
    }
}
