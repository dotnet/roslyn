// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal class SimpleIntervalTree<T, TIntrospector> : IntervalTree<T>
    where TIntrospector : struct, IIntervalIntrospector<T>
{
    private readonly TIntrospector _introspector;

    public SimpleIntervalTree(in TIntrospector introspector, IEnumerable<T>? values)
    {
        _introspector = introspector;

        if (values != null)
        {
            foreach (var value in values)
            {
                root = Insert(root, new Node(value), introspector);
            }
        }
    }

    protected ref readonly TIntrospector Introspector => ref _introspector;

    /// <summary>
    /// Warning.  Mutates the tree in place.
    /// </summary>
    /// <param name="value"></param>
    public void AddIntervalInPlace(T value)
    {
        var newNode = new Node(value);
        this.root = Insert(root, newNode, in Introspector);
    }

    /// <summary>
    /// Warning.  Mutates the tree in place.
    /// </summary>
    public void ClearInPlace()
        => this.root = null;

    public ImmutableArray<T> GetIntervalsThatOverlapWith(int start, int length)
        => GetIntervalsThatOverlapWith(start, length, in _introspector);

    public ImmutableArray<T> GetIntervalsThatIntersectWith(int start, int length)
        => GetIntervalsThatIntersectWith(start, length, in _introspector);

    public ImmutableArray<T> GetIntervalsThatContain(int start, int length)
        => GetIntervalsThatContain(start, length, in _introspector);

    public void FillWithIntervalsThatOverlapWith(int start, int length, ref TemporaryArray<T> builder)
        => FillWithIntervalsThatOverlapWith(start, length, ref builder, in _introspector);

    public void FillWithIntervalsThatIntersectWith(int start, int length, ref TemporaryArray<T> builder)
        => FillWithIntervalsThatIntersectWith(start, length, ref builder, in _introspector);

    public void FillWithIntervalsThatContain(int start, int length, ref TemporaryArray<T> builder)
        => FillWithIntervalsThatContain(start, length, ref builder, in _introspector);

    public bool HasIntervalThatIntersectsWith(int position)
        => HasIntervalThatIntersectsWith(position, in _introspector);

    public bool HasIntervalThatOverlapsWith(int start, int length)
        => HasIntervalThatOverlapsWith(start, length, in _introspector);

    public bool HasIntervalThatIntersectsWith(int start, int length)
        => HasIntervalThatIntersectsWith(start, length, in _introspector);

    public bool HasIntervalThatContains(int start, int length)
        => HasIntervalThatContains(start, length, in _introspector);

    protected int MaxEndValue(Node node)
        => GetEnd(node.MaxEndNode.Value, in _introspector);
}
