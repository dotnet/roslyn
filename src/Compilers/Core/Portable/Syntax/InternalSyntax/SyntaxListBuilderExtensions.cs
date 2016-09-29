// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxList<GreenNode> ToList(this SyntaxListBuilder builder)
        {
            return ToList<GreenNode>(builder);
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder builder) where TNode : GreenNode
        {
            if (builder == null)
            {
                return default(SyntaxList<GreenNode>);
            }

            return new SyntaxList<TNode>(builder.ToListNode());
        }
    }
}