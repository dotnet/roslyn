// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;

namespace Microsoft.DotNet.Utilities;

internal static class Extensions
{
#if !NET
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
    {
        return new HashSet<T>(source, comparer);
    }
#endif
}
