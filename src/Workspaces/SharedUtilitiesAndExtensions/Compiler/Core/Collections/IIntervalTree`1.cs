// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Generic interface used to pass in the particular interval testing operation to performed on an interval tree. For
/// example checking if an interval 'contains', 'intersects', or 'overlaps' with a requested span.  Will be erased at 
/// runtime as it will always be passed through as a generic parameter that is a struct.
/// </summary>
internal interface IIntervalTester<T, TIntrospector>
    where TIntrospector : struct, IIntervalIntrospector<T>
{
    bool Test(T value, int start, int length, in TIntrospector introspector);
}

/// <summary>
/// Base interface all interval trees need to implement to get full functionality.  Callers are not expected to use
/// these methods directly.  Instead, they are the low level building blocks that the higher level extension methods are
/// built upon. Consumers of an interval tree should use <c>.Algorithms</c> on the instance to get access to a wealth of
/// fast operations through the <see cref="IntervalTreeAlgorithms{T, TIntervalTree}"/> type.
/// </summary>
/// <remarks>
/// Iterating an interval tree will return the intervals in sorted order based on the start point of the interval.
/// </remarks>
internal interface IIntervalTree<T> : IEnumerable<T>
{
    /// <summary>
    /// Adds all intervals within the tree within the given start/length pair that match the given <paramref
    /// name="intervalTester"/> predicate.  Results are added to the <paramref name="builder"/> array.  The <paramref
    /// name="stopAfterFirst"/> indicates if the search should stop after the first interval is found.  Results will be
    /// returned in a sorted order based on the start point of the interval.
    /// </summary>
    /// <returns>The number of matching intervals found by the method.</returns>
    int FillWithIntervalsThatMatch<TIntrospector, TIntervalTester>(
        int start, int length, ref TemporaryArray<T> builder,
        in TIntrospector introspector, in TIntervalTester intervalTester,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>
        where TIntervalTester : struct, IIntervalTester<T, TIntrospector>;

    /// <summary>
    /// Practically equivalent to <see cref="FillWithIntervalsThatMatch"/> with a check that at least one item was
    /// found.  However, separated out as a separate method as implementations can often be more efficient just
    /// answering this question, versus the more complex "fill with intervals" question above.
    /// </summary>
    bool Any<TIntrospector, TIntervalTester>(int start, int length, in TIntrospector introspector, in TIntervalTester intervalTester)
        where TIntrospector : struct, IIntervalIntrospector<T>
        where TIntervalTester : struct, IIntervalTester<T, TIntrospector>;
}
