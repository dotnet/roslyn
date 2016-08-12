// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal static class SyntaxNodeCache
    {
        internal static GreenNode TryGetNode(int kind, GreenNode child1, SyntaxFactoryContext context, out int hash)
        {
            return CommonSyntaxNodeCache.TryGetNode(kind, child1, GetFlags(context), out hash);
        }

        internal static GreenNode TryGetNode(int kind, GreenNode child1, GreenNode child2, SyntaxFactoryContext context, out int hash)
        {
            return CommonSyntaxNodeCache.TryGetNode(kind, child1, child2, GetFlags(context), out hash);
        }

        internal static GreenNode TryGetNode(int kind, GreenNode child1, GreenNode child2, GreenNode child3, SyntaxFactoryContext context, out int hash)
        {
            return CommonSyntaxNodeCache.TryGetNode(kind, child1, child2, child3, GetFlags(context), out hash);
        }

        private static GreenNode.NodeFlags GetFlags(SyntaxFactoryContext context)
        {
            GreenNode.NodeFlags flags = CommonSyntaxNodeCache.GetFlags();
            flags = CSharpSyntaxNode.SetFactoryContext(flags, context);
            return flags;
        }
    }
}