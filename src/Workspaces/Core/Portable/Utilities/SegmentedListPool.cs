// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
