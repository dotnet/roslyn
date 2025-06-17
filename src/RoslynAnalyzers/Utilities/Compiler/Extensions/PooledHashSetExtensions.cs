// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.PooledObjects.Extensions
{
    internal static class PooledHashSetExtensions
    {
        public static void AddRange<T>(this PooledHashSet<T> builder, IEnumerable<T> set2)
        {
            foreach (var item in set2)
            {
                builder.Add(item);
            }
        }
    }
}
