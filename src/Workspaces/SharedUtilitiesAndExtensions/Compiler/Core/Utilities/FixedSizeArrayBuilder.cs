// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

/// <summary>
/// A bare-bones array builder, focused on the case of producing <see cref="ImmutableArray{T}"/>s where the final array
/// size is known at construction time.  In the golden path, where all the expected items are added to the builder, and
/// <see cref="MoveToImmutable"/> is called, this type is entirely garbage free.  In the non-golden path (usually
/// encountered when a cancellation token interrupts getting the final array), this will leak the intermediary array
/// created to store the results.
/// </summary>
/// <remarks>
/// This type should only be used when all of the following are true:
/// <list type="number">
/// <item>
/// The number of elements is known up front, and is fixed.  In other words, it isn't just an initial-capacity, or a
/// rough heuristic.  Rather it will always be the exact number of elements added.
/// </item>
/// <item>
/// Exactly that number of elements is actually added prior to calling <see cref="MoveToImmutable"/>.  This means no
/// patterns like "AddIfNotNull".
/// </item>
/// <item>
/// The builder will be moved to an array (see <see cref="MoveToArray"/>) or <see cref="ImmutableArray{T}"/> (see <see
/// cref="MoveToImmutable"/>).
/// </item>
/// </list>
/// If any of the above are not true (for example, the capacity is a rough hint, or the exact number of elements may not
/// match the capacity specified, or if it's intended as a scratch buffer, and won't realize a final array), then <see
/// cref="ArrayBuilder{T}.GetInstance(int, T)"/> should be used instead.
/// </remarks>
[NonCopyable]
internal struct FixedSizeArrayBuilder<T>(int capacity)
{
    private T[] _values = new T[capacity];
    private int _index;

    public void Add(T value)
        => _values[_index++] = value;

    #region AddRange overloads.  These allow us to add these collections directly, without allocating an enumerator.

    public void AddRange(ImmutableArray<T> values)
    {
        Contract.ThrowIfTrue(_index + values.Length > _values.Length);
        Array.Copy(ImmutableCollectionsMarshal.AsArray(values)!, 0, _values, _index, values.Length);
        _index += values.Length;
    }

    public void AddRange(List<T> values)
    {
        Contract.ThrowIfTrue(_index + values.Count > _values.Length);
        foreach (var v in values)
            Add(v);
    }

    public void AddRange(HashSet<T> values)
    {
        Contract.ThrowIfTrue(_index + values.Count > _values.Length);
        foreach (var v in values)
            Add(v);
    }

    public void AddRange(ArrayBuilder<T> values)
    {
        Contract.ThrowIfTrue(_index + values.Count > _values.Length);
        foreach (var v in values)
            Add(v);
    }

    #endregion

    public void AddRange(IEnumerable<T> values)
    {
        foreach (var v in values)
            Add(v);
    }

    public readonly void Sort()
        => Sort(Comparer<T>.Default);

    public readonly void Sort(IComparer<T> comparer)
    {
        if (_index > 1)
            Array.Sort(_values, 0, _index, comparer);
    }

    /// <summary>
    /// Moves the underlying buffer out of control of this type, into the returned <see cref="ImmutableArray{T}"/>. It
    /// is an error for a client of this type to specify a capacity and then attempt to call <see
    /// cref="MoveToImmutable"/> without that number of elements actually having been added to the builder.  This will
    /// throw if attempted.  This <see cref="FixedSizeArrayBuilder{T}"/> is effectively unusable once this is called.
    /// The internal buffer will reset to an empty array, meaning no more items could ever be added to it.
    /// </summary>
    public ImmutableArray<T> MoveToImmutable()
        => ImmutableCollectionsMarshal.AsImmutableArray(MoveToArray());

    public T[] MoveToArray()
    {
        Contract.ThrowIfTrue(_index != _values.Length);
        var result = _values;
        _values = Array.Empty<T>();
        _index = 0;
        return result;
    }
}
