// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal static class IntervalTree
{
    public static IntervalTree<T> Create<T, TIntrospector>(in TIntrospector introspector, params T[] values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return Create(in introspector, (IEnumerable<T>)values);
    }

    public static IntervalTree<T> Create<T, TIntrospector>(in TIntrospector introspector, IEnumerable<T> values = null)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        return IntervalTree<T>.Create(in introspector, values ?? SpecializedCollections.EmptyEnumerable<T>());
    }
}
