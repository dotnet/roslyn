// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public static Func<SyntaxToken, bool> ToLanguageSpecific(this Func<SyntaxToken, bool> predicate)
        {
            if (predicate == SyntaxToken.Any)
            {
                return SyntaxToken.Any;
            }
            else if (predicate == SyntaxToken.NonZeroWidth)
            {
                return SyntaxToken.NonZeroWidth;
            }

            return (predicate != null) ? t => predicate(t) : (Func<SyntaxToken, bool>)null;
        }

        public static Func<SyntaxTrivia, bool> ToLanguageSpecific(this Func<SyntaxTrivia, bool> predicate)
        {
            if (predicate == SyntaxTrivia.Any)
            {
                return SyntaxTrivia.Any;
            }

            return (predicate != null) ? t => predicate(t) : (Func<SyntaxTrivia, bool>)null;
        }
    }
}