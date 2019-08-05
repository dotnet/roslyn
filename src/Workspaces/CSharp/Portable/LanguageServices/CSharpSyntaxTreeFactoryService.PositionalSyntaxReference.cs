// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpSyntaxTreeFactoryServiceFactory
    {
        private partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax reference that doesn't actually hold onto the 
            /// referenced node.  Instead, enough data is held onto so that the node
            /// can be recovered and returned if necessary.
            /// </summary>
            private class PositionalSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree _tree;
                private readonly TextSpan _textSpan;
                private readonly SyntaxKind _kind;

                public PositionalSyntaxReference(SyntaxNode node)
                {
                    _tree = node.SyntaxTree;
                    _textSpan = node.Span;
                    _kind = node.Kind();

                    System.Diagnostics.Debug.Assert(_textSpan.Length > 0);
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return _tree;
                    }
                }

                public override TextSpan Span
                {
                    get
                    {
                        return _textSpan;
                    }
                }

                public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
                {
                    // Find our node going down in the tree. 
                    // Try not going deeper than needed.
                    return this.GetNode(_tree.GetRoot(cancellationToken));
                }

                public async override Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default)
                {
                    var root = await _tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    return this.GetNode(root);
                }

                private SyntaxNode GetNode(SyntaxNode root)
                {
                    var current = root;
                    var spanStart = _textSpan.Start;

                    while (current.FullSpan.Contains(spanStart))
                    {
                        if (current.Kind() == _kind && current.Span == _textSpan)
                        {
                            return current;
                        }

                        var nodeOrToken = current.ChildThatContainsPosition(spanStart);

                        // we have got a token. It means that the node is in structured trivia
                        if (nodeOrToken.IsToken)
                        {
                            return GetNodeInStructuredTrivia(current);
                        }

                        current = nodeOrToken.AsNode();
                    }

                    throw new InvalidOperationException("reference to a node that does not exist?");
                }

                private SyntaxNode GetNodeInStructuredTrivia(SyntaxNode parent)
                {
                    // Syntax references to nonterminals in structured trivia should be uncommon.
                    // Provide more efficient implementation if that is not true
                    var descendantsIntersectingSpan = parent.DescendantNodes(_textSpan, descendIntoTrivia: true);
                    return descendantsIntersectingSpan.First(node => node.IsKind(_kind) && node.Span == _textSpan);
                }
            }
        }
    }
}
