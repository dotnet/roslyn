// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Utilities;

internal static class SegmentedListPool
{
    internal static PooledObject<SegmentedList<T>> GetPooledList<T>(out SegmentedList<T> classifiedSpans)
    {
        var pooledObject = new PooledObject<SegmentedList<T>>(
            SharedPools.BigDefault<SegmentedList<T>>(),
            static p =>
            {
                var result = p.Allocate();
                result.Clear();
                return result;
            },
            static (p, list) =>
            {
                // Deliberately do not call pool.ClearAndFree for the set as we can easily have a set that goes past the
                // threshold simply with a single classified screen.  This allows reuse of those sets without causing
                // lots of **garbage.**
                list.Clear();
                p.Free(list);
            });

        classifiedSpans = pooledObject.Object;
        return pooledObject;
    }

    /// <summary>
    /// Computes a list of results based on a provided <paramref name="addItems"/> callback.  The callback is passed
    /// a <see cref="SegmentedList{T}"/> to add results to, and additional args to assist the process.  If no items
    /// are added to the list, then the <see cref="Array.Empty{T}"/> singleton will be returned.  Otherwise the 
    /// <see cref="SegmentedList{T}"/> instance will be returned.
    /// </summary>
    public static IReadOnlyList<T> ComputeList<T, TArgs>(
        Action<TArgs, SegmentedList<T>> addItems,
        TArgs args,
        // Only used to allow type inference to work at callsite
        T? _)
    {
        var pooledObject = GetPooledList<T>(out var list);

        addItems(args, list);

        // If the result was empty, return it to the pool, and just pass back the empty array singleton.
        if (pooledObject.Object.Count == 0)
        {
            pooledObject.Dispose();
            return [];
        }

        // Otherwise, do not dispose.  Caller needs this value to stay alive.
        return list;
    }
}

internal static class SegmentedListPool<T>
{
    public static IReadOnlyList<T> ComputeList<TArgs>(Action<TArgs, SegmentedList<T>> addItems, TArgs args)
        => SegmentedListPool.ComputeList(addItems, args, _: default);
}
