// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal readonly struct BlendedNode
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
