﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxListExtensions
    {
        public static SyntaxList<T> RemoveRange<T>(this SyntaxList<T> syntaxList, int index, int count) where T : SyntaxNode
        {
            var result = new List<T>(syntaxList);
            result.RemoveRange(index, count);
            return SyntaxFactory.List(result);
        }

        public static SyntaxList<T> ToSyntaxList<T>(this IEnumerable<T> sequence) where T : SyntaxNode
        {
            return SyntaxFactory.List(sequence);
        }

        public static SyntaxList<T> Insert<T>(this SyntaxList<T> list, int index, T item) where T : SyntaxNode
        {
            return list.Take(index).Concat(item).Concat(list.Skip(index)).ToSyntaxList();
        }
    }
}
