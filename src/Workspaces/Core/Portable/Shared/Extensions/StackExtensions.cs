// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class StackExtensions
    {
        public static void Push<T>(this Stack<T> stack, IEnumerable<T> values)
        {
            foreach (var v in values)
            {
                stack.Push(v);
            }
        }

        internal static void PushReverse<T, U>(this Stack<T> stack, IList<U> range)
            where U : T
        {
            Contract.ThrowIfNull(stack);
            Contract.ThrowIfNull(range);

            for (var i = range.Count - 1; i >= 0; i--)
            {
                stack.Push(range[i]);
            }
        }
    }
}
