// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// this is a basic do-nothing implementation of a syntax reference
    /// </summary>
    internal class SimpleSyntaxReference : SyntaxReference
    {
        private readonly SyntaxNode _node;

        internal SimpleSyntaxReference(SyntaxNode node)
        {
            _node = node;
        }

        public override SyntaxTree SyntaxTree
        {
            get
            {
                return _node.SyntaxTree;
            }
        }

        public override TextSpan Span
        {
            get
            {
                return _node.Span;
            }
        }

        public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
        {
            return _node;
        }
    }
}
