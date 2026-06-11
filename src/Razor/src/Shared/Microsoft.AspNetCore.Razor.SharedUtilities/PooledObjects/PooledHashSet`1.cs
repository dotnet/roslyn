// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
///  Wraps a pooled <see cref="HashSet{T}"/> but doesn't allocate it until
///  it's needed. Note: Dispose this to ensure that the pooled set is returned
///  to the pool.
/// </summary>
internal ref struct PooledHashSet<T>
{
    private readonly IEqualityComparer<T> _comparer;

    // Used in NET only code below. Called API doesn't exist on framework.
#pragma warning disable IDE0052
    private readonly int? _capacity;
#pragma warning restore IDE0052

    private HashSetPool<T>? _pool;
    private (bool hasValue, T value) _item;
    private HashSet<T>? _set;

    public PooledHashSet()
    {
        _comparer = EqualityComparer<T>.Default;
    }

    public PooledHashSet(int capacity)
    {
        _comparer = EqualityComparer<T>.Default;
        _capacity = capacity;
    }

    public PooledHashSet(IEqualityComparer<T> comparer)
    {
        ValidateComparer(comparer);
        _comparer = comparer;
    }

    public PooledHashSet(IEqualityComparer<T> comparer, int capacity)
    {
        ValidateComparer(comparer);
        _comparer = comparer;
        _capacity = capacity;
    }

    public PooledHashSet(HashSetPool<T> pool, int capacity)
    {
        _comparer = pool.Comparer;
        _pool = pool;
        _capacity = capacity;
    }

    public PooledHashSet(HashSetPool<T> pool)
    {
        _comparer = pool.Comparer;
        _pool = pool;
    }

    [Conditional("DEBUG")]
    private static void ValidateComparer(IEqualityComparer<T> comparer)
    {
        if (!ReferenceEquals(comparer, EqualityComparer<T>.Default) &&
            !ReferenceEquals(comparer, StringComparer.Ordinal) &&
            !ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
        {
            ThrowHelper.ThrowArgumentException(nameof(comparer),
                "Only EqualityComparer<T>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase are supported. Please provide a pool.");
        }
    }

    private readonly IEqualityComparer<T> Comparer => _comparer ?? EqualityComparer<T>.Default;

    private readonly bool HasSingleItem => _item.hasValue && _set is null;

    [MemberNotNullWhen(false, nameof(_set))]
    private readonly bool SetIsNullOrEmpty => _set is null || _set.Count == 0;

    public void Dispose()
    {
        ClearAndFree();
    }

    public readonly int Count
    {
        get
        {
            if (HasSingleItem)
            {
                return 1;
            }

            if (SetIsNullOrEmpty)
            {
                return 0;
            }

            return _set.Count;
        }
    }

    public bool Add(T item)
    {
        if (_set is null)
        {
            // Optimized for the single item case.
            if (!HasSingleItem)
            {
                _item.value = item;
                _item.hasValue = true;
                return true;
            }

            if (Comparer.Equals(_item.value, item))
            {
                // Duplicate of single item.
                return false;
            }

            _set = AcquireHashSet();
        }

        return _set.Add(item);
    }

    public void ClearAndFree()
    {
        if (_pool is not null && _set is { } set)
        {
            _pool.Return(set);
        }

        _set = null;
        _pool = null;
        _item = default;
    }

    public readonly bool Contains(T item)
    {
        if (_set is null)
        {
            return _item.hasValue && Comparer.Equals(_item.value, item);
        }

        return _set.Contains(item);
    }

    public readonly T[] ToArray()
    {
        if (HasSingleItem)
        {
            return [_item.value];
        }

        if (SetIsNullOrEmpty)
        {
            return [];
        }

        var result = new T[_set.Count];
        _set.CopyTo(result);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableArray()
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(ToArray());
    }

    public readonly ImmutableArray<T> OrderByAsArray<TKey>(Func<T, TKey> keySelector)
    {
        if (HasSingleItem)
        {
            return [_item.value];
        }

        if (SetIsNullOrEmpty)
        {
            return [];
        }

        return _set.OrderByAsArray(keySelector);
    }

    public void UnionWith(ImmutableArray<T> other)
    {
        if (other.IsDefaultOrEmpty)
        {
            return;
        }

        if (other.Length == 1)
        {
            Add(other[0]);
            return;
        }

        _set ??= AcquireHashSet();

        // Avoid boxing the ImmutableArray<T>
        var array = ImmutableCollectionsMarshal.AsArray(other)!;

        _set.UnionWith(array);
    }

    public void UnionWith(IReadOnlyList<T>? other)
    {
        if (other is null || other.Count == 0)
        {
            return;
        }

        if (other.Count == 1)
        {
            Add(other[0]);
            return;
        }

        _set ??= AcquireHashSet();

        _set.UnionWith(other);
    }

    private HashSet<T> AcquireHashSet()
    {
        Debug.Assert(_set is null);

        _pool ??= TrySelectPool(Comparer);

        var result = _pool is not null
            ? _pool.Get()
            : new HashSet<T>(Comparer);

#if NET
        if (_capacity is int capacity)
        {
            result.EnsureCapacity(capacity);
        }
#endif

        if (HasSingleItem)
        {
            result.Add(_item.value);
            _item = default;
        }

        return result;

        static HashSetPool<T>? TrySelectPool(IEqualityComparer<T> comparer)
        {
            if (ReferenceEquals(comparer, EqualityComparer<T>.Default))
            {
                return HashSetPool<T>.Default;
            }

            if (typeof(T) == typeof(string))
            {
                if (ReferenceEquals(comparer, StringComparer.Ordinal))
                {
                    return (HashSetPool<T>)(object)SpecializedPools.StringHashSet.Ordinal;
                }

                if (ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
                {
                    return (HashSetPool<T>)(object)SpecializedPools.StringHashSet.OrdinalIgnoreCase;
                }
            }

            return null;
        }
    }
}
