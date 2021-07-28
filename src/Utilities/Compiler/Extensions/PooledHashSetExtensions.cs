// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

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