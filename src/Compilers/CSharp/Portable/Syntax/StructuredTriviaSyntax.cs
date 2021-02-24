// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// It's a non terminal Trivia CSharpSyntaxNode that has a tree underneath it.
    /// </summary>
    public abstract partial class StructuredTriviaSyntax : CSharpSyntaxNode, IStructuredTriviaSyntax
    {
        private SyntaxTrivia _parent;

        internal StructuredTriviaSyntax(InternalSyntax.CSharpSyntaxNode green, SyntaxNode parent, int position)
            : base(green, position, parent == null ? null : parent.SyntaxTree)
        {
            System.Diagnostics.Debug.Assert(parent == null || position >= 0);
        }

        internal static StructuredTriviaSyntax Create(SyntaxTrivia trivia)
        {
            var node = trivia.UnderlyingNode;
            var parent = trivia.Token.Parent;
            var position = trivia.Position;
            var red = (StructuredTriviaSyntax)node.CreateRed(parent, position);
            red._parent = trivia;
            return red;
        }

        /// <summary>
        /// Get parent trivia.
        /// </summary>
        public override SyntaxTrivia ParentTrivia => _parent;
    }
}
