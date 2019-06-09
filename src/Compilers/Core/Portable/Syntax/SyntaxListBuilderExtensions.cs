// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxTokenList ToTokenList(this SyntaxListBuilder builder)
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SyntaxTokenList);
            }

            return new SyntaxTokenList(null, builder.ToListNode(), 0, 0);
        }

        public static SyntaxList<SyntaxNode> ToList(this SyntaxListBuilder builder)
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            return new SyntaxList<SyntaxNode>(builder.ToListNode().CreateRed());
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder builder)
            where TNode : SyntaxNode
        {
            if (builder == null || builder.Count == 0)
            {
                return new SyntaxList<TNode>();
            }

            return new SyntaxList<TNode>(builder.ToListNode().CreateRed());
        }

        public static SeparatedSyntaxList<TNode> ToSeparatedList<TNode>(this SyntaxListBuilder builder) where TNode : SyntaxNode
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SeparatedSyntaxList<TNode>);
            }

            return new SeparatedSyntaxList<TNode>(new SyntaxNodeOrTokenList(builder.ToListNode().CreateRed(), 0));
        }
    }
}
