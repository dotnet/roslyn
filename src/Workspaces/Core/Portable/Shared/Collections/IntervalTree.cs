// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class IntervalTree
    {
        public static IntervalTree<T> Create<T>(IIntervalIntrospector<T> introspector, params T[] values)
        {
            return Create(introspector, (IEnumerable<T>)values);
        }

        public static IntervalTree<T> Create<T>(IIntervalIntrospector<T> introspector, IEnumerable<T> values = null)
        {
            Contract.ThrowIfNull(introspector);
            return new IntervalTree<T>(introspector, values ?? SpecializedCollections.EmptyEnumerable<T>());
        }
    }
}
