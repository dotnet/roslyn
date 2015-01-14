// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal interface IBidirectionalMap<TKey, TValue>
    {
        bool IsEmpty { get; }

        bool TryGetValue(TKey key, out TValue value);
        bool TryGetKey(TValue value, out TKey key);

        TValue GetValueOrDefault(TKey key);
        TKey GetKeyOrDefault(TValue value);

        bool ContainsKey(TKey key);
        bool ContainsValue(TValue value);

        IBidirectionalMap<TKey, TValue> RemoveKey(TKey key);
        IBidirectionalMap<TKey, TValue> RemoveValue(TValue value);

        IBidirectionalMap<TKey, TValue> Add(TKey key, TValue value);

        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
    }
}
