// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP

namespace System
{
    internal static class StringCompatExtensions
    {
        public static string Replace(this string str, string oldValue, string? newValue, StringComparison comparisonType)
        {
            if (comparisonType != StringComparison.Ordinal)
                throw new NotSupportedException();

            return str.Replace(oldValue, newValue);
        }
    }
}

#endif
