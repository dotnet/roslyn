// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax reference that doesn't actually hold onto the 
            /// referenced node.  Instead, enough data is held onto so that the node
            /// can be recovered and returned if necessary.
            /// </summary>
            private class PathSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree _tree;
                private readonly SyntaxKind _kind;
                private readonly TextSpan _textSpan;
                private readonly ImmutableArray<int> _pathFromRoot;

                public PathSyntaxReference(SyntaxNode node)
                {
                    _tree = node.SyntaxTree;
                    _kind = node.Kind();
                    _textSpan = node.Span;
                    _pathFromRoot = ComputePathFromRoot(node);
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

                private ImmutableArray<int> ComputePathFromRoot(SyntaxNode node)
                {
                    var path = new List<int>();
                    var root = _tree.GetRoot();

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
                            throw new InvalidOperationException(CSharpWorkspaceResources.Node_does_not_descend_from_root);
                        }
                    }

                    path.Reverse();
                    return path.ToImmutableArray();
                }

                private int GetChildIndex(SyntaxNodeOrToken child)
                {
                    var parent = child.Parent;
                    var index = 0;

                    foreach (var nodeOrToken in parent.ChildNodesAndTokens())
                    {
                        if (nodeOrToken == child)
                        {
                            return index;
                        }

                        index++;
                    }

                    throw new InvalidOperationException(CSharpWorkspaceResources.Node_not_in_parent_s_child_list);
                }

                private int GetTriviaIndex(SyntaxTrivia trivia)
                {
                    var token = trivia.Token;
                    var index = 0;

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

                    throw new InvalidOperationException(CSharpWorkspaceResources.Trivia_is_not_associated_with_token);
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
                    return this.GetNode(_tree.GetRoot(cancellationToken));
                }

                public async override Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default)
                {
                    var root = await _tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    return this.GetNode(root);
                }

                private SyntaxNode GetNode(SyntaxNode root)
                {
                    var node = root;
                    for (int i = 0, n = _pathFromRoot.Length; i < n; i++)
                    {
                        var child = node.ChildNodesAndTokens()[_pathFromRoot[i]];

                        if (child.IsToken)
                        {
                            // if child is a token then we must be looking for a node in structured trivia
                            i++;
                            System.Diagnostics.Debug.Assert(i < n);
                            var triviaIndex = _pathFromRoot[i];
                            var trivia = GetTrivia(child.AsToken(), triviaIndex);
                            node = trivia.GetStructure();
                        }
                        else
                        {
                            node = child.AsNode();
                        }
                    }

                    System.Diagnostics.Debug.Assert(node.Kind() == _kind);
                    System.Diagnostics.Debug.Assert(node.Span == _textSpan);

                    return node;
                }
            }
        }
    }
}
