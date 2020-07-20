// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class FunctionExtensions
    {
        public static HashSet<T> TransitiveClosure<T>(
            this Func<T, IEnumerable<T>> relation,
            T item)
        {
            var closure = new HashSet<T>();
            var stack = new Stack<T>();
            stack.Push(item);
            while (stack.Count > 0)
            {
                T current = stack.Pop();
                foreach (var newItem in relation(current))
                {
                    if (closure.Add(newItem))
                    {
                        stack.Push(newItem);
                    }
                }
            }

            return closure;
        }
    }
}
