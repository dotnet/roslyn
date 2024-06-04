// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Base interface all interval trees need to implement to get full functionality.  Callers are not expected to use
/// these methods directly.  Instead, they are the low level building blocks that the higher level extension methods are
/// built upon.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IIntervalTree<T>
{
    /// <summary>
    /// Adds all intervals within the tree within the given start/length pair that match the given <paramref
    /// name="testInterval"/> predicate.  Results are added to the <paramref name="builder"/> array.  The <paramref
    /// name="stopAfterFirst"/> indicates if the search should stop after the first interval is found.  Results will be
    /// returned in a sorted order based on the start point of the interval.
    /// </summary>
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
