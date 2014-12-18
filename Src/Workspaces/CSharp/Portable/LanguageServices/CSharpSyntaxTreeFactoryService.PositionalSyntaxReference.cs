// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax reference that doesn't actually hold onto the 
            /// referenced node.  Instead, enough data is held onto so that the node
            /// can be recovered and returned if necessary.
            /// </summary>
            private class PositionalSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree tree;
                private readonly TextSpan textSpan;
                private readonly SyntaxKind kind;

                public PositionalSyntaxReference(SyntaxNode node)
                {
                    this.tree = node.SyntaxTree;
                    this.textSpan = node.Span;
                    this.kind = node.Kind();

                    System.Diagnostics.Debug.Assert(textSpan.Length > 0);
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return tree;
                    }
                }

                public override TextSpan Span
                {
                    get
                    {
                        return textSpan;
                    }
                }

                public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
                {
                    // Find our node going down in the tree. 
                    // Try not going deeper than needed.
                    return this.GetNode(tree.GetRoot(cancellationToken));
                }

                public async override Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    return this.GetNode(root);
                }

                private SyntaxNode GetNode(SyntaxNode root)
                {
                    var current = root;
                    int spanStart = this.textSpan.Start;

                    while (current.FullSpan.Contains(spanStart))
                    {
                        if (current.Kind() == this.kind && current.Span == this.textSpan)
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
                    var descendantsIntersectingSpan = parent.DescendantNodes(this.textSpan, descendIntoTrivia: true);
                    return descendantsIntersectingSpan.First((node) => node.IsKind(this.kind) && node.Span == this.textSpan);
                }
            }
        }
    }
}