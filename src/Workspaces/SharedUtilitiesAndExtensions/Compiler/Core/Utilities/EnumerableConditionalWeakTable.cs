// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

#if NET
// Can't use global alias due to generic parameters. Extension types would do.

internal readonly struct EnumerableConditionalWeakTable<TKey, TValue>() : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : class
    where TValue : class
{
    private readonly ConditionalWeakTable<TKey, TValue> _table = new();

    public object WriteLock => _table;

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        => _table.TryGetValue(key, out value);

    public void Add(TKey key, TValue value)
        => _table.Add(key, value);

    public void AddOrUpdate(TKey key, TValue value)
        => _table.AddOrUpdate(key, value);

    public bool Remove(TKey key)
        => _table.Remove(key);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<TKey, TValue>>)_table).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
#else
internal sealed class EnumerableConditionalWeakTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : class
    where TValue : class
{
    private sealed class Box(TKey key, TValue value)
    {
        public readonly TKey Key = key;
        public readonly TValue Value = value;
    }

    private readonly ConditionalWeakTable<TKey, Box> _table = new();
    private ImmutableList<WeakReference<Box>> _items = [];

    public object WriteLock => _table;

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_table.TryGetValue(key, out var box))
        {
            value = box.Value;
            return true;
        }

        value = null;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        lock (WriteLock)
        {
            AddNoLock(key, value);

            // clean up collected objects:
            _items = _items.RemoveAll(WeakReferenceExtensions.IsNull);
        }
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        lock (WriteLock)
        {
            _ = RemoveNoLock(key);
            AddNoLock(key, value);
        }
    }

    public bool Remove(TKey key)
    {
        lock (WriteLock)
        {
            return RemoveNoLock(key);
        }
    }

    private void AddNoLock(TKey key, TValue value)
    {
        var box = new Box(key, value);
        _table.Add(key, box);
        _items = _items.Add(new WeakReference<Box>(box));
    }

    private bool RemoveNoLock(TKey key)
    {
        if (!_table.TryGetValue(key, out var box))
        {
            return false;
        }

        Contract.ThrowIfFalse(_table.Remove(key));
        _items = _items.RemoveAll(item => !item.TryGetTarget(out var target) || ReferenceEquals(target, box));
        return true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var item in _items)
        {
            if (item.TryGetTarget(out var box))
            {
                yield return KeyValuePair.Create(box.Key, box.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
#endif

