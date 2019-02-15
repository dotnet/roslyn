// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
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