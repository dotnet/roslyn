// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Generic function representing the type of interval testing operation that can be performed on an interval tree. For
/// example checking if an interval 'contains', 'intersects', or 'overlaps' with a requested span.
/// </summary>
internal delegate bool TestInterval<T, TIntrospector>(T value, int start, int length, in TIntrospector introspector)
    where TIntrospector : struct, IIntervalIntrospector<T>;

/// <summary>
/// Base interface all interval trees need to implement to get full functionality.  Callers are not expected to use
/// these methods directly.  Instead, they are the low level building blocks that the higher level extension methods are
/// built upon. Consumers of an interface tree should use <c>.Algorithms</c> on the instance to get access to a wealth
/// of fast operations through the <see cref="IntervalTreeAlgorithms{T, TIntervalTree}"/> type.
/// </summary>
internal interface IIntervalTree<T>
{
    /// <summary>
    /// Adds all intervals within the tree within the given start/length pair that match the given <paramref
    /// name="testInterval"/> predicate.  Results are added to the <paramref name="builder"/> array.  The <paramref
    /// name="stopAfterFirst"/> indicates if the search should stop after the first interval is found.  Results will be
    /// returned in a sorted order based on the start point of the interval.
    /// </summary>
    /// <returns>The number of matching intervals found by the method.</returns>
    int FillWithIntervalsThatMatch<TIntrospector>(
        int start, int length, TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder, in TIntrospector introspector,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>;

    /// <summary>
    /// Practically equivalent to <see cref="FillWithIntervalsThatMatch{TIntrospector}"/> with a check that at least one
    /// item was found.  However, separated out as a separate method as implementations can often be more efficient just
    /// answering this question, versus the more complex "full with intervals" question above.
    /// </summary>
    bool Any<TIntrospector>(int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>;
}

internal readonly struct FlatArrayIntervalTree<T> : IIntervalTree<T>
{
    private readonly record struct Node(T Value, int MaxEndNodeIndex);

    public static readonly FlatArrayIntervalTree<T> Empty = new(new SegmentedArray<Node>(0));

    private readonly SegmentedArray<Node> _array;

    private FlatArrayIntervalTree(SegmentedArray<Node> array)
    {
        _array = array;
    }

    /// <summary>
    /// Creates a <see cref="FlatArrayIntervalTree{T}"/> from an unsorted list of <paramref name="values"/>.  This will
    /// incur a delegate allocation to sort the values.  If callers can avoid that allocation by pre-sorting the values,
    /// they should do so and call <see cref="CreateFromSorted"/> instead.
    /// </summary>
    /// <remarks>
    /// <paramref name="values"/> will be sorted in place.
    /// </remarks>
    public static FlatArrayIntervalTree<T> CreateFromUnsorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var localIntrospector = introspector;
        values.Sort((t1, t2) => localIntrospector.GetSpan(t1).Start - localIntrospector.GetSpan(t2).Start);
        return CreateFromSorted(introspector, values);
    }

    /// <summary>
    /// Creates an interval tree from a sorted list of values.  This is more efficient than creating from an unsorted
    /// list as building doesn't need to figure out where the nodes need to go n-log(n) and doesn't have to rebalance
    /// anything (again, another n-log(n) operation).  Rebalancing is particularly expensive as it involves tons of
    /// pointer chasing operations, which is both slow, and which impacts the GC which has to track all those writes.
    /// </summary>
    /// <remarks>
    /// The values must be sorted such that given any two elements 'a' and 'b' in the list, if 'a' comes before 'b' in
    /// the list, then it's "start position" (as determined by the introspector) must be less than or equal to 'b's
    /// start position.  This is a requirement for the algorithm to work correctly.
    /// </remarks>
    public static FlatArrayIntervalTree<T> CreateFromSorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
#if DEBUG
        var localIntrospector = introspector;
        Debug.Assert(values.IsSorted(Comparer<T>.Create((t1, t2) => localIntrospector.GetSpan(t1).Start - localIntrospector.GetSpan(t2).Start)));
#endif

        if (values.Count == 0)
            return Empty;

        var array = new SegmentedArray<Node>(values.Count);
        CreateFromSortedWorker(values, startInclusive: 0, endExclusive: values.Count, destination: array, destinationIndex: 0, in introspector);
        return new FlatArrayIntervalTree<T>(array);
    }

    private static void CreateFromSortedWorker<TIntrospector>(
        SegmentedList<T> values,
        int startInclusive,
        int endExclusive,
        SegmentedArray<Node> destination,
        int destinationIndex,
        in TIntrospector introspector) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var length = endExclusive - startInclusive;
        if (length <= 0)
            return;

        // Start in the middle of the range of values we were asked to look at.
        var mid = startInclusive + (length >> 1);
        var midValue = values[mid];

        // Process the left side.  Everything from the start up to the mid.
        var leftChildDestinationIndex = (2 * destinationIndex) + 1;
        CreateFromSortedWorker(values, startInclusive, mid, destination, destinationIndex: leftChildDestinationIndex, in introspector);

        // Process the right side.  Everything after the mid up to the end.
        var rightChildDestinationIndex = (2 * destinationIndex) + 2;
        CreateFromSortedWorker(values, mid + 1, endExclusive, destination, destinationIndex: rightChildDestinationIndex, in introspector);

        var thisEndValue = GetEnd(midValue, in introspector);
        var leftEndValue = MaxEndValue(destination, leftChildDestinationIndex, in introspector);
        var rightEndValue = MaxEndValue(destination, rightChildDestinationIndex, in introspector);

        int maxEndNodeIndex;
        if (thisEndValue >= leftEndValue && thisEndValue >= rightEndValue)
        {
            maxEndNodeIndex = destinationIndex;
        }
        else if ((leftEndValue >= rightEndValue) && leftChildDestinationIndex < destination.Length)
        {
            maxEndNodeIndex = destination[leftChildDestinationIndex].MaxEndNodeIndex;
        }
        else if (rightChildDestinationIndex < destination.Length)
        {
            maxEndNodeIndex = destination[rightChildDestinationIndex].MaxEndNodeIndex;
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }

        destination[destinationIndex] = new Node(midValue, maxEndNodeIndex);
    }

    private static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;

    private static int MaxEndValue<TIntrospector>(SegmentedArray<Node> nodes, int index, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => index >= nodes.Length ? 0 : GetEnd(nodes[nodes[index].MaxEndNodeIndex].Value, in introspector);
}
