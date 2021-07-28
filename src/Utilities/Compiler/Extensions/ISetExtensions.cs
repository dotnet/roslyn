// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Collections.Generic
{
    internal static class ISetExtensions
    {
        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                set.Add(value);
            }
        }
    }
}