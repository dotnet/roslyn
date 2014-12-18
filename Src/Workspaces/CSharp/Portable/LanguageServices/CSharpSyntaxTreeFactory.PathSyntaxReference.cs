// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
            private class PathSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree tree;
                private readonly SyntaxKind kind;
                private readonly TextSpan textSpan;
                private readonly ImmutableArray<int> pathFromRoot;

                public PathSyntaxReference(SyntaxNode node)
                {
                    this.tree = node.SyntaxTree;
                    this.kind = node.CSharpKind();
                    this.textSpan = node.Span;
                    this.pathFromRoot = ComputePathFromRoot(node);
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return this.tree;
                    }
                }

                public override TextSpan Span
                {
                    get
                    {
                        return this.textSpan;
                    }
                }

                private ImmutableArray<int> ComputePathFromRoot(SyntaxNode node)
                {
                    var path = new List<int>();
                    var root = tree.GetRoot();

                    while (node != root)
                    {
                        for (; node.Parent != null; node = node.Parent)
                        {
                            var index = GetChildIndex(node);
                            path.Add(index);
                        }

                        // if we were part of structure trivia, continue searching until we get to the true root
                        if (node.IsStructuredTrivia)
                        {
                            var trivia = node.ParentTrivia;
                            var triviaIndex = GetTriviaIndex(trivia);
                            path.Add(triviaIndex);
                            var tokenIndex = GetChildIndex(trivia.Token);
                            path.Add(tokenIndex);
                            node = trivia.Token.Parent;
                            continue;
                        }
                        else if (node != root)
                        {
                            throw new InvalidOperationException(CSharpWorkspaceResources.NodeDoesNotDescendFromRoo);
                        }
                    }

                    path.Reverse();
                    return path.ToImmutableArray();
                }

                private int GetChildIndex(SyntaxNodeOrToken child)
                {
                    var parent = child.Parent;
                    int index = 0;

                    foreach (var nodeOrToken in parent.ChildNodesAndTokens())
                    {
                        if (nodeOrToken == child)
                        {
                            return index;
                        }

                        index++;
                    }

                    throw new InvalidOperationException(CSharpWorkspaceResources.NodeNotInParentsChildLis);
                }

                private int GetTriviaIndex(SyntaxTrivia trivia)
                {
                    var token = trivia.Token;
                    int index = 0;

                    foreach (var tr in token.LeadingTrivia)
                    {
                        if (tr == trivia)
                        {
                            return index;
                        }

                        index++;
                    }

                    foreach (var tr in token.TrailingTrivia)
                    {
                        if (tr == trivia)
                        {
                            return index;
                        }

                        index++;
                    }

                    throw new InvalidOperationException(CSharpWorkspaceResources.TriviaIsNotAssociatedWith);
                }

                private SyntaxTrivia GetTrivia(SyntaxToken token, int triviaIndex)
                {
                    var leadingCount = token.LeadingTrivia.Count;
                    if (triviaIndex <= leadingCount)
                    {
                        return token.LeadingTrivia.ElementAt(triviaIndex);
                    }

                    triviaIndex -= leadingCount;
                    return token.TrailingTrivia.ElementAt(triviaIndex);
                }

                public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
                {
                    return this.GetNode(this.tree.GetRoot(cancellationToken));
                }

                public async override Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    var root = await this.tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    return this.GetNode(root);
                }

                private SyntaxNode GetNode(SyntaxNode root)
                {
                    var node = root;
                    for (int i = 0, n = this.pathFromRoot.Length; i < n; i++)
                    {
                        var child = node.ChildNodesAndTokens()[this.pathFromRoot[i]];

                        if (child.IsToken)
                        {
                            // if child is a token then we must be looking for a node in structured trivia
                            i++;
                            System.Diagnostics.Debug.Assert(i < n);
                            var triviaIndex = this.pathFromRoot[i];
                            var trivia = GetTrivia(child.AsToken(), triviaIndex);
                            node = trivia.GetStructure();
                        }
                        else
                        {
                            node = child.AsNode();
                        }
                    }

                    System.Diagnostics.Debug.Assert(node.CSharpKind() == this.kind);
                    System.Diagnostics.Debug.Assert(node.Span == this.textSpan);

                    return node;
                }
            }
        }
    }
}