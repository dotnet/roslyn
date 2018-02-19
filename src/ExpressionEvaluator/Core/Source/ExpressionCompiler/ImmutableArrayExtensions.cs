// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
