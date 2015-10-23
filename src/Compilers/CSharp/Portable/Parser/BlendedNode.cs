// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal struct BlendedNode
    {
        internal readonly CSharp.CSharpSyntaxNode Node;
        internal readonly SyntaxToken Token;
        internal readonly Blender Blender;

        internal BlendedNode(CSharp.CSharpSyntaxNode node, SyntaxToken token, Blender blender)
        {
            this.Node = node;
            this.Token = token;
            this.Blender = blender;
        }
    }
}
