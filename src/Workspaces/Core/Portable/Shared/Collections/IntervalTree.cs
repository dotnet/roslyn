// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
