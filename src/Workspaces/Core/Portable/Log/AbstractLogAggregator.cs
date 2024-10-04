// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// helper class to aggregate some numeric value log in client side
/// </summary>
internal abstract class AbstractLogAggregator<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
    where TValue : class // TValue being constrained to a class will ensure that the ConcurrentDictionaries won't be JITted with structs
{
    /// <remarks>
    /// The key here is an object even though we will often be putting enums into this map; the problem with the use of enums or other value
    /// types is they prevent the runtime from sharing the same JITted code for each different generic instantiation. In this case,
    /// the cost of boxing is cheaper than the cost of the extra JIT.
    /// </remarks>
    private readonly ConcurrentDictionary<object, TValue> _map = new(concurrencyLevel: 2, capacity: 2);
    private readonly Func<object, TValue> _createCounter;

    protected AbstractLogAggregator()
    {
        _createCounter = _ => CreateCounter();
    }

    protected abstract TValue CreateCounter();

    public bool IsEmpty => _map.IsEmpty;

    public void Clear() => _map.Clear();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => _map.Select(static kvp => KeyValuePairUtil.Create((TKey)kvp.Key, kvp.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1279909", AllowCaptures = false)]
    protected TValue GetCounter(TKey key)
        => _map.GetOrAdd((object)key, _createCounter);

    protected bool TryGetCounter(TKey key, [MaybeNullWhen(false)] out TValue counter)
    {
        if (_map.TryGetValue((object)key, out counter))
        {
            return true;
        }

        return false;
    }
}
