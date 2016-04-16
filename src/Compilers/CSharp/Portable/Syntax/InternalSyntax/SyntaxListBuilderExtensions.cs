// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxList<SyntaxToken> ToTokenList(this SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<SyntaxToken>);
            }

            return new SyntaxList<SyntaxToken>(builder.ToListNode());
        }

        public static SyntaxList<CSharpSyntaxNode> ToList(this SyntaxListBuilder builder)
        {
            return ToList<CSharpSyntaxNode>(builder);
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder builder) where TNode : CSharpSyntaxNode
        {
            if (builder == null)
            {
                return default(SyntaxList<CSharpSyntaxNode>);
            }

            return new SyntaxList<TNode>(builder.ToListNode());
        }

        public static SeparatedSyntaxList<TNode> ToSeparatedList<TNode>(this SyntaxListBuilder builder) where TNode : CSharpSyntaxNode
        {
            if (builder == null)
            {
                return default(SeparatedSyntaxList<TNode>);
            }

            return ToList(builder).AsSeparatedList<TNode>();
        }
    }
}
