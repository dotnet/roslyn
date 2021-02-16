// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal interface IBidirectionalMap<TKey, TValue>
    {
        bool IsEmpty { get; }

        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
        bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key);

        TValue? GetValueOrDefault(TKey key);
        TKey? GetKeyOrDefault(TValue value);

        bool ContainsKey(TKey key);
        bool ContainsValue(TValue value);

        IBidirectionalMap<TKey, TValue> RemoveKey(TKey key);
        IBidirectionalMap<TKey, TValue> RemoveValue(TValue value);

        IBidirectionalMap<TKey, TValue> Add(TKey key, TValue value);

        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
    }
}
