// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class TemporaryArrayExtensions
    {
        public static bool Any<T>(this in TemporaryArray<T> array, Func<T, bool> predicate)
        {
            foreach (var item in array)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        public static bool All<T>(this in TemporaryArray<T> array, Func<T, bool> predicate)
        {
            foreach (var item in array)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        public static void AddIfNotNull<T>(this ref TemporaryArray<T> array, T? value)
            where T : struct
        {
            if (value is not null)
            {
                array.Add(value.Value);
            }
        }

        public static void AddIfNotNull<T>(this ref TemporaryArray<T> array, T? value)
            where T : class
        {
            if (value is not null)
            {
                array.Add(value);
            }
        }
    }
}
