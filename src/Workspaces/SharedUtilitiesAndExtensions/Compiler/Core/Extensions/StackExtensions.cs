// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
