// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
