// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class ImmutableArrayExtensions
    {
        internal static int IndexOf<TItem, TArg>(this ImmutableArray<TItem> array, Func<TItem, TArg, bool> predicate, TArg arg)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i], arg))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
