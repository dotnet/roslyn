// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
