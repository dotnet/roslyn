// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxReplacer
    {
        internal static SyntaxNode Replace<TNode>(
            SyntaxNode root,
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
            where TNode : SyntaxNode
        {
            var replacer = new Replacer<TNode>(
                nodes, computeReplacementNode,
                tokens, computeReplacementToken,
                trivia, computeReplacementTrivia);

            if (replacer.HasWork)
            {
                return replacer.RewriteNode(root);
            }
            else
            {
                return root;
            }
        }

        internal static SyntaxToken Replace(
            SyntaxToken root,
            IEnumerable<SyntaxNode> nodes = null,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
        {
            var replacer = new Replacer<SyntaxNode>(
                nodes, computeReplacementNode,
                tokens, computeReplacementToken,
                trivia, computeReplacementTrivia);

            if (replacer.HasWork)
            {
                return replacer.RewriteToken(root);
            }
            else
            {
                return root;
            }
        }

        private class Replacer<TNode> : CSharpBottomUpSyntaxRewriter where TNode : SyntaxNode
        {
            private readonly Func<TNode, TNode, SyntaxNode> _computeReplacementNode;
            private readonly Func<SyntaxToken, SyntaxToken, SyntaxToken> _computeReplacementToken;
            private readonly Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> _computeReplacementTrivia;

            private readonly HashSet<SyntaxNode> _nodeSet;
            private readonly HashSet<SyntaxToken> _tokenSet;
            private readonly HashSet<SyntaxTrivia> _triviaSet;
            private readonly HashSet<TextSpan> _spanSet;

            private readonly TextSpan _totalSpan;
            private readonly bool _visitIntoStructuredTrivia;
            private readonly bool _shouldVisitTrivia;

            public Replacer(
                IEnumerable<TNode> nodes,
                Func<TNode, TNode, SyntaxNode> computeReplacementNode,
                IEnumerable<SyntaxToken> tokens,
                Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken,
                IEnumerable<SyntaxTrivia> trivia,
                Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            {
                _computeReplacementNode = computeReplacementNode;
                _computeReplacementToken = computeReplacementToken;
                _computeReplacementTrivia = computeReplacementTrivia;

                _nodeSet = nodes != null ? new HashSet<SyntaxNode>(nodes) : s_noNodes;
                _tokenSet = tokens != null ? new HashSet<SyntaxToken>(tokens) : s_noTokens;
                _triviaSet = trivia != null ? new HashSet<SyntaxTrivia>(trivia) : s_noTrivia;

                _spanSet = new HashSet<TextSpan>(
                    _nodeSet.Select(n => n.FullSpan).Concat(
                    _tokenSet.Select(t => t.FullSpan).Concat(
                    _triviaSet.Select(t => t.FullSpan))));

                _totalSpan = ComputeTotalSpan(_spanSet);

                _visitIntoStructuredTrivia =
                    _nodeSet.Any(n => n.IsPartOfStructuredTrivia()) ||
                    _tokenSet.Any(t => t.IsPartOfStructuredTrivia()) ||
                    _triviaSet.Any(t => t.IsPartOfStructuredTrivia());

                _shouldVisitTrivia = _triviaSet.Count > 0 || _visitIntoStructuredTrivia;
            }

            private static readonly HashSet<SyntaxNode> s_noNodes = new HashSet<SyntaxNode>();
            private static readonly HashSet<SyntaxToken> s_noTokens = new HashSet<SyntaxToken>();
            private static readonly HashSet<SyntaxTrivia> s_noTrivia = new HashSet<SyntaxTrivia>();

            public override bool VisitIntoStructuredTrivia
            {
                get
                {
                    return _visitIntoStructuredTrivia;
                }
            }

            public bool HasWork
            {
                get
                {
                    return _nodeSet.Count + _tokenSet.Count + _triviaSet.Count > 0;
                }
            }

            private static TextSpan ComputeTotalSpan(IEnumerable<TextSpan> spans)
            {
                bool first = true;
                int start = 0;
                int end = 0;

                foreach (var span in spans)
                {
                    if (first)
                    {
                        start = span.Start;
                        end = span.End;
                        first = false;
                    }
                    else
                    {
                        start = Math.Min(start, span.Start);
                        end = Math.Max(end, span.End);
                    }
                }

                return new TextSpan(start, end - start);
            }

            private bool ShouldVisit(TextSpan span)
            {
                // first do quick check against total span
                if (!span.IntersectsWith(_totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var s in _spanSet)
                {
                    if (span.Contains(s))
                    {
                        // node's full span intersects with at least one node to be replaced
                        // so we need to visit node's children to find it.
                        return true;
                    }
                }

                return false;
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
                return _shouldVisitTrivia && ShouldVisit(trivia.FullSpan);
            }

            public override SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
            {
                if (_nodeSet.Contains(original) && _computeReplacementNode != null)
                {
                    rewritten = _computeReplacementNode((TNode)original, (TNode)rewritten);
                }

                return rewritten;
            }

            public override SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
            {
                if (_tokenSet.Contains(original) && _computeReplacementToken != null)
                {
                    rewritten = _computeReplacementToken(original, rewritten);
                }

                return rewritten;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia original, SyntaxTrivia rewritten)
            {
                if (_triviaSet.Contains(original) && _computeReplacementTrivia != null)
                {
                    rewritten = _computeReplacementTrivia(original, rewritten);
                }

                return rewritten;
            }
        }

        internal static SyntaxNode ReplaceNodeInList(SyntaxNode root, SyntaxNode originalNode, IEnumerable<SyntaxNode> newNodes)
        {
            return new NodeListEditor(originalNode, newNodes, ListEditKind.Replace).RewriteNode(root);
        }

        internal static SyntaxNode InsertNodeInList(SyntaxNode root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return new NodeListEditor(nodeInList, nodesToInsert, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).RewriteNode(root);
        }

        public static SyntaxNode ReplaceTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
        {
            return new TokenListEditor(tokenInList, newTokens, ListEditKind.Replace).RewriteNode(root);
        }

        public static SyntaxNode InsertTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        {
            return new TokenListEditor(tokenInList, newTokens, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).RewriteNode(root);
        }

        public static SyntaxNode ReplaceTriviaInList(SyntaxNode root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia)
        {
            return new TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).RewriteNode(root);
        }

        public static SyntaxNode InsertTriviaInList(SyntaxNode root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
        {
            return new TriviaListEditor(triviaInList, newTrivia, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).RewriteNode(root);
        }

        public static SyntaxToken ReplaceTriviaInList(SyntaxToken root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia)
        {
            return new TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).RewriteToken(root);
        }

        public static SyntaxToken InsertTriviaInList(SyntaxToken root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
        {
            return new TriviaListEditor(triviaInList, newTrivia, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).RewriteToken(root);
        }

        private enum ListEditKind
        {
            InsertBefore,
            InsertAfter,
            Replace
        }

        private static InvalidOperationException GetItemNotListElementException()
        {
            return new InvalidOperationException(CodeAnalysisResources.MissingListItem);
        }

        private abstract class BaseListEditor : CSharpBottomUpSyntaxRewriter
        {
            private readonly TextSpan _elementSpan;
            private readonly bool _visitTrivia;
            private readonly bool _visitIntoStructuredTrivia;

            protected readonly ListEditKind editKind;

            public BaseListEditor(
                TextSpan elementSpan,
                ListEditKind editKind,
                bool visitTrivia,
                bool visitIntoStructuredTrivia)
            {
                _elementSpan = elementSpan;
                this.editKind = editKind;
                _visitTrivia = visitTrivia || visitIntoStructuredTrivia;
                _visitIntoStructuredTrivia = visitIntoStructuredTrivia;
            }

            public override bool VisitIntoStructuredTrivia
            {
                get
                {
                    return _visitIntoStructuredTrivia;
                }
            }

            private bool ShouldVisit(TextSpan span)
            {
                if (span.Contains(_elementSpan))
                {
                    // node's full span intersects with at least one node to be replaced
                    // so we need to visit node's children to find it.
                    return true;
                }

                return false;
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
                return ShouldVisit(trivia.FullSpan);
            }
        }

        private class NodeListEditor : BaseListEditor
        {
            private readonly SyntaxNode _originalNode;
            private readonly IEnumerable<SyntaxNode> _newNodes;
            private bool _seen;
            private bool _changed;

            public NodeListEditor(
                SyntaxNode originalNode,
                IEnumerable<SyntaxNode> replacementNodes,
                ListEditKind editKind)
                : base(originalNode.Span, editKind, false, originalNode.IsPartOfStructuredTrivia())
            {
                _originalNode = originalNode;
                _newNodes = replacementNodes;
            }

            public new SyntaxNode RewriteNode(SyntaxNode node)
            {
                var result = base.RewriteNode(node);

                if (_seen && !_changed)
                {
                    throw GetItemNotListElementException();
                }

                return result;
            }

            public override SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
            {
                if (original == _originalNode)
                {
                    _seen = true;
                }

                return rewritten;
            }

            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> original, SeparatedSyntaxList<TNode> rewritten)
            {
                if (_originalNode is TNode)
                {
                    var index = original.IndexOf((TNode)_originalNode);
                    if (index >= 0 && index < original.Count)
                    {
                        _changed = true;

                        switch (this.editKind)
                        {
                            case ListEditKind.Replace:
                                return rewritten.ReplaceRange(rewritten[index], _newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return rewritten.InsertRange(index + 1, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return rewritten.InsertRange(index, _newNodes.Cast<TNode>());
                        }
                    }
                }

                return rewritten;
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> original, SyntaxList<TNode> rewritten)
            {
                if (_originalNode is TNode)
                {
                    var index = original.IndexOf((TNode)_originalNode);
                    if (index >= 0 && index < original.Count)
                    {
                        _changed = true;

                        switch (this.editKind)
                        {
                            case ListEditKind.Replace:
                                return rewritten.ReplaceRange(rewritten[index], _newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return rewritten.InsertRange(index + 1, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return rewritten.InsertRange(index, _newNodes.Cast<TNode>());
                        }
                    }
                }

                return rewritten;
            }
        }

        private class TokenListEditor : BaseListEditor
        {
            private readonly SyntaxToken _originalToken;
            private readonly IEnumerable<SyntaxToken> _newTokens;
            private bool _seen;
            private bool _changed;

            public TokenListEditor(
                SyntaxToken originalToken,
                IEnumerable<SyntaxToken> newTokens,
                ListEditKind editKind)
                : base(originalToken.Span, editKind, false, originalToken.IsPartOfStructuredTrivia())
            {
                _originalToken = originalToken;
                _newTokens = newTokens;
            }

            public new SyntaxNode RewriteNode(SyntaxNode node)
            {
                var result = base.RewriteNode(node);

                if (_seen && !_changed)
                {
                    throw GetItemNotListElementException();
                }

                return result;
            }

            public override SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
            {
                if (original == _originalToken)
                {
                    _seen = true;
                }

                return rewritten;
            }

            public override SyntaxTokenList VisitList(SyntaxTokenList original, SyntaxTokenList rewritten)
            {
                var index = original.IndexOf(_originalToken);
                if (index >= 0 && index < original.Count)
                {
                    _changed = true;

                    switch (this.editKind)
                    {
                        case ListEditKind.Replace:
                            return rewritten.ReplaceRange(rewritten[index], _newTokens);

                        case ListEditKind.InsertAfter:
                            return rewritten.InsertRange(index + 1, _newTokens);

                        case ListEditKind.InsertBefore:
                            return rewritten.InsertRange(index, _newTokens);
                    }
                }

                return rewritten;
            }
        }

        private class TriviaListEditor : BaseListEditor
        {
            private readonly SyntaxTrivia _originalTrivia;
            private readonly IEnumerable<SyntaxTrivia> _newTrivia;
            private bool _seen;
            private bool _changed;

            public TriviaListEditor(
                SyntaxTrivia originalTrivia,
                IEnumerable<SyntaxTrivia> newTrivia,
                ListEditKind editKind)
                : base(originalTrivia.Span, editKind, true, originalTrivia.IsPartOfStructuredTrivia())
            {
                _originalTrivia = originalTrivia;
                _newTrivia = newTrivia;
            }

            public new SyntaxNode RewriteNode(SyntaxNode node)
            {
                var result = base.RewriteNode(node);

                if (_seen && !_changed)
                {
                    throw GetItemNotListElementException();
                }

                return result;
            }

            public new SyntaxToken RewriteToken(SyntaxToken token)
            {
                var result = base.RewriteToken(token);

                if (_seen && !_changed)
                {
                    throw GetItemNotListElementException();
                }

                return result;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia original, SyntaxTrivia rewritten)
            {
                if (original == _originalTrivia)
                {
                    _seen = true;
                }

                return rewritten;
            }

            public override SyntaxTriviaList VisitList(SyntaxTriviaList original, SyntaxTriviaList rewritten)
            {
                var index = original.IndexOf(_originalTrivia);
                if (index >= 0 && index < original.Count)
                {
                    _changed = true;

                    switch (this.editKind)
                    {
                        case ListEditKind.Replace:
                            return rewritten.ReplaceRange(rewritten[index], _newTrivia);

                        case ListEditKind.InsertAfter:
                            return rewritten.InsertRange(index + 1, _newTrivia);

                        case ListEditKind.InsertBefore:
                            return rewritten.InsertRange(index, _newTrivia);
                    }
                }

                return rewritten;
            }
        }
    }
}
