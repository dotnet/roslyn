// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP

namespace System
{
    internal static class StringCompatExtensions
    {
        extension(string str)
        {
            public bool Contains(string value, StringComparison comparisonType)
            {
                return str.IndexOf(value, comparisonType) >= 0;
            }

            public string Replace(string oldValue, string? newValue, StringComparison comparisonType)
            {
                if (comparisonType != StringComparison.Ordinal)
                    throw new NotSupportedException();

                return str.Replace(oldValue, newValue);
            }
        }
    }
}

#endif
