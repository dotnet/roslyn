// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
