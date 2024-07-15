// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Roslyn.Utilities;

internal class BidirectionalMap<TKey, TValue> : IBidirectionalMap<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    public static readonly IBidirectionalMap<TKey, TValue> Empty =
        new BidirectionalMap<TKey, TValue>(ImmutableDictionary.Create<TKey, TValue>(), ImmutableDictionary.Create<TValue, TKey>());

    private readonly ImmutableDictionary<TKey, TValue> _forwardMap;
    private readonly ImmutableDictionary<TValue, TKey> _backwardMap;

    public BidirectionalMap(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
        _forwardMap = ImmutableDictionary.CreateRange<TKey, TValue>(pairs);
        _backwardMap = ImmutableDictionary.CreateRange<TValue, TKey>(pairs.Select(p => KeyValuePairUtil.Create(p.Value, p.Key)));
    }

    private BidirectionalMap(ImmutableDictionary<TKey, TValue> forwardMap, ImmutableDictionary<TValue, TKey> backwardMap)
    {
        _forwardMap = forwardMap;
        _backwardMap = backwardMap;
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        => _forwardMap.TryGetValue(key, out value);

    public bool TryGetKey(TValue value, [NotNullWhen(true)] out TKey? key)
        => _backwardMap.TryGetValue(value, out key);

    public bool ContainsKey(TKey key)
        => _forwardMap.ContainsKey(key);

    public bool ContainsValue(TValue value)
        => _backwardMap.ContainsKey(value);

    public IBidirectionalMap<TKey, TValue> RemoveKey(TKey key)
    {
        if (!_forwardMap.TryGetValue(key, out var value))
        {
            return this;
        }

        return new BidirectionalMap<TKey, TValue>(
            _forwardMap.Remove(key),
            _backwardMap.Remove(value));
    }

    public IBidirectionalMap<TKey, TValue> RemoveValue(TValue value)
    {
        if (!_backwardMap.TryGetValue(value, out var key))
        {
            return this;
        }

        return new BidirectionalMap<TKey, TValue>(
            _forwardMap.Remove(key),
            _backwardMap.Remove(value));
    }

    public IBidirectionalMap<TKey, TValue> Add(TKey key, TValue value)
    {
        return new BidirectionalMap<TKey, TValue>(
            _forwardMap.Add(key, value),
            _backwardMap.Add(value, key));
    }

    public IEnumerable<TKey> Keys => _forwardMap.Keys;

    public IEnumerable<TValue> Values => _backwardMap.Keys;

    public bool IsEmpty
    {
        get
        {
            return _backwardMap.Count == 0;
        }
    }

    public int Count
    {
        get
        {
            Debug.Assert(_forwardMap.Count == _backwardMap.Count);
            return _backwardMap.Count;
        }
    }

    public TValue? GetValueOrDefault(TKey key)
    {
        if (TryGetValue(key, out var result))
        {
            return result;
        }

        return default;
    }

    public TKey? GetKeyOrDefault(TValue value)
    {
        if (TryGetKey(value, out var result))
        {
            return result;
        }

        return default;
    }

    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }

            throw new KeyNotFoundException();
        }
    }

    public TKey this[TValue value]
    {
        get
        {
            if (TryGetKey(value, out var result))
            {
                return result;
            }

            throw new KeyNotFoundException();
        }
    }
}
