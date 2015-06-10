// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxNodeRemover
    {
        internal static TRoot RemoveNodes<TRoot>(TRoot root,
                IEnumerable<SyntaxNode> nodes,
                SyntaxRemoveOptions options)
            where TRoot : SyntaxNode
        {
            if (nodes == null)
            {
                return root;
            }

            var nodeArray = nodes.ToArray();

            if (nodeArray.Length == 0)
            {
                return root;
            }

            var remover = new SyntaxRemover(nodes.ToArray(), options);
            return (TRoot)remover.RewriteNode(root);
        }

        private class SyntaxRemover : CSharpBottomUpSyntaxRewriter
        {
            private readonly HashSet<SyntaxNode> _nodesToRemove;
            private readonly SyntaxRemoveOptions _options;
            private readonly TextSpan _searchSpan;
            private SyntaxTriviaList _residualTrivia;
            private HashSet<SyntaxNode> _directivesToKeep;

            public SyntaxRemover(
                SyntaxNode[] nodesToRemove,
                SyntaxRemoveOptions options)
                : base(nodesToRemove.Any(n => n.IsPartOfStructuredTrivia()))
            {
                _nodesToRemove = new HashSet<SyntaxNode>(nodesToRemove);
                _options = options;
                _searchSpan = ComputeTotalSpan(nodesToRemove);
                _residualTrivia = default(SyntaxTriviaList);
            }

            private static TextSpan ComputeTotalSpan(SyntaxNode[] nodes)
            {
                var span0 = nodes[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;

                for (int i = 1; i < nodes.Length; i++)
                {
                    var span = nodes[i].FullSpan;
                    start = Math.Min(start, span.Start);
                    end = Math.Max(end, span.End);
                }

                return new TextSpan(start, end - start);
            }

            private static bool IsEndOfLine(SyntaxTrivia trivia)
            {
                return trivia.Kind() == SyntaxKind.EndOfLineTrivia
                    || trivia.Kind() == SyntaxKind.SingleLineCommentTrivia
                    || trivia.IsDirective;
            }

            private static bool HasEndOfLine(SyntaxTriviaList list)
            {
                return list.Any(t => IsEndOfLine(t));
            }

            private bool IsForRemoval(SyntaxNode node)
            {
                return _nodesToRemove.Contains(node);
            }

            private bool ShouldVisit(TextSpan span)
            {
                return span.IntersectsWith(_searchSpan) || (_residualTrivia != null && _residualTrivia.Count > 0);
            }

            public override bool CanVisit(SyntaxNode node)
            {
                return ShouldVisit(node.FullSpan);
            }

            public override bool CanVisit(SyntaxToken token)
            {
                return ShouldVisit(token.FullSpan);
            }

            public override bool CanVisit(SyntaxTrivia trivia)
            {
                return this.VisitIntoStructuredTrivia && ShouldVisit(trivia.FullSpan);
            }

            public override SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
            {
                // if any residual trivia exists, then it must have come from this node's last child being removed
                // (otherwise any residual trivia would already be incorporated into the rewritten node)
                // so it should be attached as trailing trivia.
                rewritten = WithTrailingResidualTrivia(rewritten);

                if (this.IsForRemoval(original))
                {
                    _residualTrivia = AddTrivia(_residualTrivia, rewritten);
                    return null;
                }
                else
                {
                    return rewritten;
                }
            }

            public override SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
            {
                // attach any residual trivia from removal of prior sibling (or ancestors' prior sibling) to this token
                return WithLeadingResidualTrivia(rewritten);
            }

            public override SyntaxNode VisitListElement(SyntaxNode original, SyntaxNode rewritten)
            {
                // don't call VisitNode so we can distinguish between child nodes and list element nodes
                return rewritten;
            }

            public override SyntaxToken VisitListSeparator(SyntaxToken original, SyntaxToken rewritten)
            {
                // don't call VisitToken so we can distinguish between child tokens and list separator tokens
                return rewritten;
            }

            public override SyntaxToken VisitListElement(SyntaxToken original, SyntaxToken rewritten)
            {
                // don't call VisitToken so we can distinguish between child tokens and list element tokens
                return rewritten;
            }

            public override SyntaxTrivia VisitListElement(SyntaxTrivia original, SyntaxTrivia rewritten)
            {
                return rewritten;
            }

            // rewrite list without removed nodes
            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> original, SyntaxList<TNode> rewritten)
            {
                List<TNode> alternate = null;

                for (int i = 0, n = original.Count; i < n; i++)
                {
                    var originalItem = original[i];
                    var rewrittenItem = rewritten[i];
                    TNode visited = null;

                    if (IsForRemoval(originalItem))
                    {
                        _residualTrivia = AddTrivia(_residualTrivia, rewrittenItem);
                    }
                    else
                    {
                        visited = (TNode)WithLeadingResidualTrivia(rewrittenItem);
                    }

                    if (rewrittenItem != visited && alternate == null)
                    {
                        alternate = new List<TNode>(n);
                        alternate.AddRange(rewritten.Take(i));
                    }

                    if (visited != null && alternate != null)
                    {
                        alternate.Add(visited);
                    }
                }

                if (alternate != null)
                {
                    return SyntaxFactory.List(alternate);
                }
                else
                {
                    return rewritten;
                }
            }

            // rewrite separated list without removed nodes and associated separators
            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> original, SeparatedSyntaxList<TNode> rewritten)
            {
                var originalWithSeps = original.GetWithSeparators();
                var rewrittenWithSeps = rewritten.GetWithSeparators();
                bool removeNextSeparator = false;

                SyntaxNodeOrTokenListBuilder alternate = null;
                for (int i = 0, n = originalWithSeps.Count; i < n; i++)
                {
                    var originalItem = originalWithSeps[i];
                    var rewrittenItem = rewrittenWithSeps[i];
                    SyntaxNodeOrToken visited;

                    if (originalItem.IsToken) // separator
                    {
                        if (removeNextSeparator)
                        {
                            removeNextSeparator = false;
                            visited = default(SyntaxNodeOrToken);
                        }
                        else
                        {
                            visited = WithLeadingResidualTrivia(rewrittenItem.AsToken());
                        }
                    }
                    else
                    {
                        var originalNode = (TNode)originalItem.AsNode();
                        var rewrittenNode = (TNode)rewrittenItem.AsNode();

                        if (this.IsForRemoval(originalNode))
                        {
                            if (alternate == null)
                            {
                                alternate = new SyntaxNodeOrTokenListBuilder(n);
                                alternate.Add(rewrittenWithSeps, 0, i);
                            }

                            if (alternate.Count > 0 && alternate[alternate.Count - 1].IsToken)
                            {
                                // remove preceding separator if any
                                var separator = alternate[alternate.Count - 1].AsToken();
                                _residualTrivia = AddTrivia(_residualTrivia, separator, rewrittenNode);
                                alternate.RemoveLast();
                            }
                            else if (i + 1 < n && rewrittenWithSeps[i + 1].IsToken)
                            {
                                // otherwise remove following separator if any
                                var separator = rewrittenWithSeps[i + 1].AsToken();
                                _residualTrivia = AddTrivia(_residualTrivia, rewrittenNode, separator);
                                removeNextSeparator = true;
                            }
                            else
                            {
                                _residualTrivia = AddTrivia(_residualTrivia, rewrittenNode);
                            }

                            visited = default(SyntaxNodeOrToken);
                        }
                        else
                        {
                            visited = WithLeadingResidualTrivia(rewrittenItem.AsNode());
                        }
                    }

                    if (rewrittenItem != visited && alternate == null)
                    {
                        alternate = new SyntaxNodeOrTokenListBuilder(n);
                        alternate.Add(rewrittenWithSeps, 0, i);
                    }

                    if (alternate != null && visited.Kind() != SyntaxKind.None)
                    {
                        alternate.Add(visited);
                    }
                }

                if (alternate != null)
                {
                    return alternate.ToList().AsSeparatedList<TNode>();
                }

                return rewritten;
            }

            private SyntaxNode WithLeadingResidualTrivia(SyntaxNode node)
            {
                if (node != null && _residualTrivia.Count > 0)
                {
                    node = node.WithLeadingTrivia(_residualTrivia.AddRange(node.GetLeadingTrivia()));
                    _residualTrivia = default(SyntaxTriviaList);
                }

                return node;
            }

            private SyntaxNode WithTrailingResidualTrivia(SyntaxNode node)
            {
                if (node != null && _residualTrivia.Count > 0)
                {
                    node = node.WithTrailingTrivia(node.GetTrailingTrivia().AddRange(_residualTrivia));
                    _residualTrivia = default(SyntaxTriviaList);
                }

                return node;
            }

            private SyntaxToken WithLeadingResidualTrivia(SyntaxToken token)
            {
                if (!token.IsKind(SyntaxKind.None) && _residualTrivia.Count > 0)
                {
                    token = token.WithLeadingTrivia(_residualTrivia.AddRange(token.LeadingTrivia));
                    _residualTrivia = default(SyntaxTriviaList);
                }

                return token;
            }

            private SyntaxTriviaList AddEndOfLine(SyntaxTriviaList list)
            {
                if (list.Count == 0 || !IsEndOfLine(list[list.Count -1]))
                {
                    list = list.Add(SyntaxFactory.CarriageReturnLineFeed);
                }

                return list;
            }

            private SyntaxTriviaList AddTrivia(SyntaxTriviaList list, SyntaxNode node)
            {
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    list = list.AddRange(node.GetLeadingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && HasEndOfLine(node.GetLeadingTrivia()))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                {
                    list = AddDirectives(list, node, GetRemovedSpan(node.Span, node.FullSpan));
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    list = list.AddRange(node.GetTrailingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && HasEndOfLine(node.GetTrailingTrivia()))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                {
                    list = list.Add(SyntaxFactory.ElasticMarker);
                }

                return list;
            }

            private SyntaxTriviaList AddTrivia(SyntaxTriviaList list, SyntaxToken token, SyntaxNode node)
            {
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    list = list.AddRange(token.LeadingTrivia)
                               .AddRange(token.TrailingTrivia)
                               .AddRange(node.GetLeadingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && (HasEndOfLine(token.LeadingTrivia) ||
                        HasEndOfLine(token.TrailingTrivia) ||
                        HasEndOfLine(node.GetLeadingTrivia())))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                {
                    var span = TextSpan.FromBounds(token.Span.Start, node.Span.End);
                    var fullSpan = TextSpan.FromBounds(token.FullSpan.Start, node.FullSpan.End);
                    list = AddDirectives(list, node.Parent, GetRemovedSpan(span, fullSpan));
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    list = list.AddRange(node.GetTrailingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && HasEndOfLine(node.GetTrailingTrivia()))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                {
                    list = list.Add(SyntaxFactory.ElasticMarker);
                }

                return list;
            }

            private SyntaxTriviaList AddTrivia(SyntaxTriviaList list, SyntaxNode node, SyntaxToken token)
            {
                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    list = list.AddRange(node.GetLeadingTrivia());
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && HasEndOfLine(node.GetLeadingTrivia()))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & (SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.KeepUnbalancedDirectives)) != 0)
                {
                    var span = TextSpan.FromBounds(node.Span.Start, token.Span.End);
                    var fullSpan = TextSpan.FromBounds(node.FullSpan.Start, token.FullSpan.End);
                    list = AddDirectives(list, node.Parent, GetRemovedSpan(span, fullSpan));
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    list = list.AddRange(node.GetTrailingTrivia())
                               .AddRange(token.LeadingTrivia)
                               .AddRange(token.TrailingTrivia);
                }
                else if ((_options & SyntaxRemoveOptions.KeepEndOfLine) != 0
                    && (HasEndOfLine(node.GetTrailingTrivia()) ||
                        HasEndOfLine(token.LeadingTrivia) ||
                        HasEndOfLine(token.TrailingTrivia)))
                {
                    list = AddEndOfLine(list);
                }

                if ((_options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                {
                    list = list.Add(SyntaxFactory.ElasticMarker);
                }

                return list;
            }

            private TextSpan GetRemovedSpan(TextSpan span, TextSpan fullSpan)
            {
                var removedSpan = fullSpan;

                if ((_options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    removedSpan = TextSpan.FromBounds(span.Start, removedSpan.End);
                }

                if ((_options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    removedSpan = TextSpan.FromBounds(removedSpan.Start, span.End);
                }

                return removedSpan;
            }

            private SyntaxTriviaList AddDirectives(SyntaxTriviaList list, SyntaxNode node, TextSpan span)
            {
                if (node.ContainsDirectives)
                {
                    if (_directivesToKeep == null)
                    {
                        _directivesToKeep = new HashSet<SyntaxNode>();
                    }
                    else
                    {
                        _directivesToKeep.Clear();
                    }

                    var directivesInSpan = node.DescendantTrivia(span, n => n.ContainsDirectives, descendIntoTrivia: true)
                                         .Where(tr => tr.IsDirective)
                                         .Select(tr => (DirectiveTriviaSyntax)tr.GetStructure());

                    foreach (var directive in directivesInSpan)
                    {
                        if ((_options & SyntaxRemoveOptions.KeepDirectives) != 0)
                        {
                            _directivesToKeep.Add(directive);
                        }
                        else if (directive.Kind() == SyntaxKind.DefineDirectiveTrivia ||
                            directive.Kind() == SyntaxKind.UndefDirectiveTrivia)
                        {
                            // always keep #define and #undef, even if we are only keeping unbalanced directives
                            _directivesToKeep.Add(directive);
                        }
                        else if (HasRelatedDirectives(directive))
                        {
                            // a balanced directive with respect to a given node has all related directives rooted under that node
                            var relatedDirectives = directive.GetRelatedDirectives();
                            var balanced = relatedDirectives.All(rd => rd.FullSpan.OverlapsWith(span));

                            if (!balanced)
                            {
                                // if not fully balanced, all related directives under the node are considered unbalanced.
                                foreach (var unbalancedDirective in relatedDirectives.Where(rd => rd.FullSpan.OverlapsWith(span)))
                                {
                                    _directivesToKeep.Add(unbalancedDirective);
                                }
                            }
                        }

                        if (_directivesToKeep.Contains(directive))
                        {
                            list = AddEndOfLine(list.Add(directive.ParentTrivia));
                        }
                    }
                }

                return list;
            }

            private static bool HasRelatedDirectives(DirectiveTriviaSyntax directive)
            {
                switch (directive.Kind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.ElifDirectiveTrivia:
                    case SyntaxKind.EndIfDirectiveTrivia:
                    case SyntaxKind.RegionDirectiveTrivia:
                    case SyntaxKind.EndRegionDirectiveTrivia:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
