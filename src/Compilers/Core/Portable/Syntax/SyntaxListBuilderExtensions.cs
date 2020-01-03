// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Syntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxTokenList ToTokenList(this SyntaxListBuilder? builder)
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SyntaxTokenList);
            }

            return new SyntaxTokenList(null, builder.ToListNode(), 0, 0);
        }

        public static SyntaxList<SyntaxNode> ToList(this SyntaxListBuilder? builder)
        {
            var listNode = builder?.ToListNode();
            if (listNode is null)
            {
                return default;
            }

            return new SyntaxList<SyntaxNode>(listNode.CreateRed());
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder? builder)
            where TNode : SyntaxNode
        {
            var listNode = builder?.ToListNode();
            if (listNode is null)
            {
                return default;
            }

            return new SyntaxList<TNode>(listNode.CreateRed());
        }

        public static SeparatedSyntaxList<TNode> ToSeparatedList<TNode>(this SyntaxListBuilder? builder) where TNode : SyntaxNode
        {
            var listNode = builder?.ToListNode();
            if (listNode is null)
            {
                return default;
            }

            return new SeparatedSyntaxList<TNode>(new SyntaxNodeOrTokenList(listNode.CreateRed(), 0));
        }
    }
}
