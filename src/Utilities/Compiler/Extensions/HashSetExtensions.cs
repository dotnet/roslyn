// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace System.Collections.Generic
{
    internal static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                set.Add(value);
            }
        }
    }
}