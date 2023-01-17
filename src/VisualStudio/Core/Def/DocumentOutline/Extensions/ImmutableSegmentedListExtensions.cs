// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal static class ImmutableSegmentedListExtensions
    {
        public static TResult? SelectLastNonNullOrDefault<TSource, TResult>(this ImmutableSegmentedList<TSource> source, Func<TSource, TResult?> selector)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = selector(source[i]);
                if (item is not null)
                {
                    return item;
                }
            }

            return default;
        }
    }
}
