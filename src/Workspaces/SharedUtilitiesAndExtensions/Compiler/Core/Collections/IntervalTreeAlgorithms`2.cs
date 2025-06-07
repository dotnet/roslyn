// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Helpers for working with <see cref="IIntervalTree{T}"/> instances.  Can be retrieved by calling <c>.Extensions</c>
/// on an interval tree instance.  This is exposed as a struct instead of extension methods as the type inference
/// involved here is too complex for C# to handle (specifically using a <c>TIntervalTree</c> type), which would make
/// ergonomics extremely painful as the callsites would have to pass three type arguments along explicitly.
/// </summary>
internal readonly struct IntervalTreeAlgorithms<T, TIntervalTree>(TIntervalTree tree) where TIntervalTree : IIntervalTree<T>
{
    public ImmutableArray<T> GetIntervalsThatMatch<TIntrospector, TIntervalTester>(
        int start, int length, in TIntrospector introspector, in TIntervalTester intervalTester)
        where TIntrospector : struct, IIntervalIntrospector<T>
        where TIntervalTester : struct, IIntervalTester<T, TIntrospector>
    {
        using var result = TemporaryArray<T>.Empty;
        tree.FillWithIntervalsThatMatch(start, length, ref result.AsRef(), in introspector, in intervalTester, stopAfterFirst: false);
        return result.ToImmutableAndClear();
    }

    public ImmutableArray<T> GetIntervalsThatOverlapWith<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return GetIntervalsThatMatch(start, length, in introspector, default(Tests<TIntrospector>.OverlapsWithIntervalTester));
    }

    public ImmutableArray<T> GetIntervalsThatIntersectWith<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return GetIntervalsThatMatch(start, length, in introspector, default(Tests<TIntrospector>.IntersectsWithIntervalTester));
    }

    public ImmutableArray<T> GetIntervalsThatContain<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return GetIntervalsThatMatch(start, length, in introspector, default(Tests<TIntrospector>.ContainsIntervalTester));
    }

    public void FillWithIntervalsThatOverlapWith<TIntrospector>(
        int start, int length, ref TemporaryArray<T> builder, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        tree.FillWithIntervalsThatMatch(start, length, ref builder, in introspector, default(Tests<TIntrospector>.OverlapsWithIntervalTester), stopAfterFirst: false);
    }

    public void FillWithIntervalsThatIntersectWith<TIntrospector>(
        int start, int length, ref TemporaryArray<T> builder, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        tree.FillWithIntervalsThatMatch(start, length, ref builder, in introspector, default(Tests<TIntrospector>.IntersectsWithIntervalTester), stopAfterFirst: false);
    }

    public void FillWithIntervalsThatContain<TIntrospector>(
        int start, int length, ref TemporaryArray<T> builder, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        tree.FillWithIntervalsThatMatch(start, length, ref builder, in introspector, default(Tests<TIntrospector>.ContainsIntervalTester), stopAfterFirst: false);
    }

    public bool HasIntervalThatIntersectsWith<TIntrospector>(
        int position, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return HasIntervalThatIntersectsWith<TIntrospector>(position, 0, in introspector);
    }

    public bool HasIntervalThatIntersectsWith<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return tree.Any(start, length, in introspector, default(Tests<TIntrospector>.IntersectsWithIntervalTester));
    }

    public bool HasIntervalThatOverlapsWith<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return tree.Any(start, length, in introspector, default(Tests<TIntrospector>.OverlapsWithIntervalTester));
    }

    public bool HasIntervalThatContains<TIntrospector>(
        int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return tree.Any(start, length, in introspector, default(Tests<TIntrospector>.ContainsIntervalTester));
    }

    public static bool Contains<TIntrospector>(T value, int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var otherStart = start;
        var otherEnd = start + length;

        var thisSpan = introspector.GetSpan(value);
        var thisStart = thisSpan.Start;
        var thisEnd = thisSpan.End;

        // TODO(cyrusn): This doesn't actually seem to match what TextSpan.Contains does.  It doesn't specialize empty
        // length in any way.  Preserving this behavior for now, but we should consider changing this.
        if (length == 0)
        {
            return thisStart <= otherStart && otherEnd < thisEnd;
        }

        return thisStart <= otherStart && otherEnd <= thisEnd;
    }

    private static bool IntersectsWith<TIntrospector>(T value, int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var otherStart = start;
        var otherEnd = start + length;

        var thisSpan = introspector.GetSpan(value);
        var thisStart = thisSpan.Start;
        var thisEnd = thisSpan.End;

        return otherStart <= thisEnd && otherEnd >= thisStart;
    }

    private static bool OverlapsWith<TIntrospector>(T value, int start, int length, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var otherStart = start;
        var otherEnd = start + length;

        var thisSpan = introspector.GetSpan(value);
        var thisStart = thisSpan.Start;
        var thisEnd = thisSpan.End;

        // TODO(cyrusn): This doesn't actually seem to match what TextSpan.OverlapsWith does.  It doesn't specialize empty
        // length in any way.  Preserving this behavior for now, but we should consider changing this.
        if (length == 0)
            return thisStart < otherStart && otherStart < thisEnd;

        var overlapStart = Math.Max(thisStart, otherStart);
        var overlapEnd = Math.Min(thisEnd, otherEnd);

        return overlapStart < overlapEnd;
    }

    private static class Tests<TIntrospector>
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        public readonly struct ContainsIntervalTester : IIntervalTester<T, TIntrospector>
        {
            public bool Test(T value, int start, int length, in TIntrospector introspector)
                => Contains(value, start, length, in introspector);
        }

        public readonly struct IntersectsWithIntervalTester : IIntervalTester<T, TIntrospector>
        {
            public bool Test(T value, int start, int length, in TIntrospector introspector)
                => IntersectsWith(value, start, length, in introspector);
        }

        public readonly struct OverlapsWithIntervalTester : IIntervalTester<T, TIntrospector>
        {
            public bool Test(T value, int start, int length, in TIntrospector introspector)
                => OverlapsWith(value, start, length, in introspector);
        }
    }
}
