// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal static class HashSetExtensions
    {
        internal static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this HashSet<T>? hashSet)
        {
            return hashSet == null || hashSet.Count == 0;
        }

        internal static bool InitializeAndAdd<T>([NotNullIfNotNull(parameterName: nameof(item)), NotNullWhen(returnValue: true)] ref HashSet<T>? hashSet, [NotNullWhen(returnValue: true)] T? item)
            where T : class
        {
            if (item is null)
            {
                return false;
            }
            else if (hashSet is null)
            {
                hashSet = new HashSet<T>();
            }

            return hashSet.Add(item);
        }

        /// <summary>
        /// This extension method is added so that it's preferred over LINQ's Any.
        /// This is more efficient than LINQ, especially in that it avoids the enumerator boxing allocation.
        /// </summary>
        internal static bool Any<T>(this HashSet<T> hashSet, Func<T, bool> predicate)
        {
            foreach (var item in hashSet)
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
