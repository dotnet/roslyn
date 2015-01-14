// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxTokenListExtensions
    {
        public static SyntaxTokenList ToSyntaxTokenList(this IEnumerable<SyntaxToken> sequence)
        {
            return SyntaxFactory.TokenList(sequence.Aggregate(new List<SyntaxToken>(), (list, token) => { list.Add(token); return list; }));
        }

        public static IEnumerable<SyntaxToken> SkipKinds(this SyntaxTokenList tokenList, params SyntaxKind[] kinds)
        {
            return tokenList.SkipWhile(t => t.IsKind(kinds));
        }
    }
}
