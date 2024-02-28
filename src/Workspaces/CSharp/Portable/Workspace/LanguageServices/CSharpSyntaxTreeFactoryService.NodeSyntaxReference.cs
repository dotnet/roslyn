// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class CSharpSyntaxTreeFactoryService
{
    internal sealed class NodeSyntaxReference : SyntaxReference
    {
        private readonly SyntaxNode _node;

        internal NodeSyntaxReference(SyntaxNode node)
            => _node = node;

        public override SyntaxTree SyntaxTree
            => _node.SyntaxTree;

        public override TextSpan Span
            => _node.Span;

        public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
            => _node;
    }
}
