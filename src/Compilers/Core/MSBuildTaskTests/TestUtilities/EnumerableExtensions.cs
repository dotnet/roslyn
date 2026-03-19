// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<S> SelectWithIndex<T, S>(this IEnumerable<T> items, Func<T, int, S> selector)
        {
            int i = 0;
            foreach (var item in items)
            {
                yield return selector(item, i++);
            }
        }
    }
}
