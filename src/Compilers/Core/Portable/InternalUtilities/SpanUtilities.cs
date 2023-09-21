// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    internal static class SpanUtilities
    {
        public static bool All<TElement, TParam>(this ReadOnlySpan<TElement> span, TParam param, Func<TElement, TParam, bool> predicate)
        {
            foreach (var e in span)
            {
                if (!predicate(e, param))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool All<TElement>(this ReadOnlySpan<TElement> span, Func<TElement, bool> predicate)
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

        /// <summary>
        /// Returns <see cref="ReadOnlySpan{T}"/> on platforms that have full support for <see cref="Span{T}"/> APIs,
        /// <see cref="string"/> otherwise.
        /// </summary>
#if NETSTANDARD2_0
        public unsafe static string AsSpanOrString(this ReadOnlySpan<char> span)
        {
            fixed (char* c = span)
            {
                return new string(c, 0, span.Length);
            }
        }
#else
        public static ReadOnlySpan<char> AsSpanOrString(this ReadOnlySpan<char> span)
            => span;
#endif
    }
}
