// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal static class CommonSyntaxListBuilderExtensions
    {
        public static CommonSyntaxList<GreenNode> ToTokenList(this CommonSyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(CommonSyntaxList<GreenNode>);
            }

            return new CommonSyntaxList<GreenNode>(builder.ToListNode());
        }

        public static CommonSyntaxList<GreenNode> ToList(this CommonSyntaxListBuilder builder)
        {
            return ToList<GreenNode>(builder);
        }

        public static CommonSyntaxList<TNode> ToList<TNode>(this CommonSyntaxListBuilder builder) where TNode : GreenNode
        {
            if (builder == null)
            {
                return default(CommonSyntaxList<GreenNode>);
            }

            return new CommonSyntaxList<TNode>(builder.ToListNode());
        }

        //public static SeparatedSyntaxList<TNode> ToSeparatedList<TNode>(this SyntaxListBuilder builder) where TNode : CSharpSyntaxNode
        //{
        //    if (builder == null)
        //    {
        //        return default(SeparatedSyntaxList<TNode>);
        //    }

        //    return ToList(builder).AsSeparatedList<TNode>();
        //}
    }
}