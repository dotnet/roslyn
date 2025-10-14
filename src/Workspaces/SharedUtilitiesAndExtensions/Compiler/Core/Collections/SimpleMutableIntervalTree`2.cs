// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal class SimpleMutableIntervalTree<T, TIntrospector> : MutableIntervalTree<T>
    where TIntrospector : struct, IIntervalIntrospector<T>
{
    private readonly TIntrospector _introspector;

    public SimpleMutableIntervalTree(in TIntrospector introspector, IEnumerable<T>? values = null)
    {
        _introspector = introspector;

        if (values != null)
        {
            foreach (var value in values)
                root = Insert(root, new Node(value), introspector);
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

    public ImmutableArray<T> GetIntervalsThatOverlapWith(int start, int length)
        => this.Algorithms.GetIntervalsThatOverlapWith(start, length, in _introspector);

    public ImmutableArray<T> GetIntervalsThatIntersectWith(int start, int length)
        => this.Algorithms.GetIntervalsThatIntersectWith(start, length, in _introspector);

    public ImmutableArray<T> GetIntervalsThatContain(int start, int length)
        => this.Algorithms.GetIntervalsThatContain(start, length, in _introspector);

    public void FillWithIntervalsThatOverlapWith(int start, int length, ref TemporaryArray<T> builder)
        => this.Algorithms.FillWithIntervalsThatOverlapWith(start, length, ref builder, in _introspector);

    public void FillWithIntervalsThatIntersectWith(int start, int length, ref TemporaryArray<T> builder)
        => this.Algorithms.FillWithIntervalsThatIntersectWith(start, length, ref builder, in _introspector);

    public void FillWithIntervalsThatContain(int start, int length, ref TemporaryArray<T> builder)
        => this.Algorithms.FillWithIntervalsThatContain(start, length, ref builder, in _introspector);

    public bool HasIntervalThatIntersectsWith(int position)
        => this.Algorithms.HasIntervalThatIntersectsWith(position, in _introspector);

    public bool HasIntervalThatOverlapsWith(int start, int length)
        => this.Algorithms.HasIntervalThatOverlapsWith(start, length, in _introspector);

    public bool HasIntervalThatIntersectsWith(int start, int length)
        => this.Algorithms.HasIntervalThatIntersectsWith(start, length, in _introspector);

    public bool HasIntervalThatContains(int start, int length)
        => this.Algorithms.HasIntervalThatContains(start, length, in _introspector);

    protected int MaxEndValue(Node node)
        => GetEnd(node.MaxEndNode.Value, in _introspector);
}
