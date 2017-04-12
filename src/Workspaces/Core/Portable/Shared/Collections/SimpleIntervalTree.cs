// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal class SimpleIntervalTree
    {
        public static SimpleIntervalTree<T> Create<T>(IIntervalIntrospector<T> introspector, params T[] values)
        {
            return Create(introspector, (IEnumerable<T>)values);
        }

        public static SimpleIntervalTree<T> Create<T>(IIntervalIntrospector<T> introspector, IEnumerable<T> values = null)
        {
            return new SimpleIntervalTree<T>(introspector, values);
        }
    }
}
