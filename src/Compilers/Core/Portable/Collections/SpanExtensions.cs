// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class SpanExtensions
    {
        public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        {
            foreach (var el in span)
            {
                if (predicate(el))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
