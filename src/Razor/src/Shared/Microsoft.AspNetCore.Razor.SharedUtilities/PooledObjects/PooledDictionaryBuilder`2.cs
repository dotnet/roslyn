// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
///  Wraps a pooled <see cref="ImmutableDictionary{TKey, TValue}.Builder"/> but doesn't allocate it until
///  it's needed. Note: Dispose this to ensure that the pooled array builder is returned
///  to the pool.
/// </summary>
internal ref struct PooledDictionaryBuilder<TKey, TValue>(DictionaryBuilderPool<TKey, TValue>? pool)
    where TKey : notnull
{
    private readonly DictionaryBuilderPool<TKey, TValue> _pool = pool ?? DictionaryBuilderPool<TKey, TValue>.Default;

    private ImmutableDictionary<TKey, TValue>.Builder? _builder;

    public PooledDictionaryBuilder()
        : this(pool: null)
    {
    }

    public void Dispose()
    {
        ClearAndFree();
    }

    public TValue this[TKey key]
    {
        readonly get
        {
            if (_builder is null)
            {
                throw new InvalidOperationException();
            }

            return _builder[key];
        }
        set
        {
            _builder ??= _pool.Get();
            _builder[key] = value;
        }
    }

    public readonly int Count
        => _builder?.Count ?? 0;

    public void Add(TKey key, TValue value)
    {
        _builder ??= _pool.Get();
        _builder.Add(key, value);
    }

    public void AddRange(IReadOnlyList<KeyValuePair<TKey, TValue>> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        _builder ??= _pool.Get();
        _builder.AddRange(items);
    }

    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        _builder ??= _pool.Get();
        _builder.AddRange(items);
    }

    public void ClearAndFree()
    {
        if (_builder is { } builder)
        {
            _pool.Return(builder);
            _builder = null;
        }
    }

    public bool ContainsKey(TKey key)
        => _builder?.ContainsKey(key) ?? false;

    public readonly void Remove(TKey key)
    {
        if (_builder is null)
        {
            throw new IndexOutOfRangeException();
        }

        _builder.Remove(key);
    }

    public readonly ImmutableDictionary<TKey, TValue> ToImmutable()
        => _builder?.ToImmutable() ?? ImmutableDictionary<TKey, TValue>.Empty;
}
