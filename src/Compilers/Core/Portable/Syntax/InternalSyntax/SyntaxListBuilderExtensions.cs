// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxList<GreenNode> ToList(this SyntaxListBuilder? builder)
        {
            return ToList<GreenNode>(builder);
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder? builder) where TNode : GreenNode
        {
            if (builder == null)
            {
                return default(SyntaxList<GreenNode>);
            }

            return new SyntaxList<TNode>(builder.ToListNode());
        }
    }
}
