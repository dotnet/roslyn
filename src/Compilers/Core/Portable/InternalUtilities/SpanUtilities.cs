// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static class SpanExtensions
    {
        public static bool All<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        {
            foreach (var e in span)
            {
                if (!predicate(e))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
