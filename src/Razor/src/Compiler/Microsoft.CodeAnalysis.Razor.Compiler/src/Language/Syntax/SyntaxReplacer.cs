// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxReplacer
{
    internal static SyntaxNode Replace<TNode>(
        SyntaxNode root,
        IEnumerable<TNode>? nodes = null,
        Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<SyntaxToken>? tokens = null,
        Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
        where TNode : SyntaxNode
    {
        var replacer = new Replacer<TNode>(nodes, computeReplacementNode, tokens, computeReplacementToken);

        if (replacer.HasWork)
        {
            return replacer.Visit(root);
        }
        else
        {
            return root;
        }
    }

    internal static SyntaxToken Replace(
        SyntaxToken root,
        IEnumerable<SyntaxNode>? nodes = null,
        Func<SyntaxNode, SyntaxNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<SyntaxToken>? tokens = null,
        Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
    {
        var replacer = new Replacer<SyntaxNode>(nodes, computeReplacementNode, tokens, computeReplacementToken);

        if (replacer.HasWork)
        {
            return replacer.VisitToken(root);
        }
        else
        {
            return root;
        }
    }

    private sealed class Replacer<TNode> : SyntaxRewriter where TNode : SyntaxNode
    {
        private static readonly HashSet<SyntaxNode> s_noNodes = [];
        private static readonly HashSet<SyntaxToken> s_noTokens = [];

        private readonly Func<TNode, TNode, SyntaxNode>? _computeReplacementNode;
        private readonly Func<SyntaxToken, SyntaxToken, SyntaxToken>? _computeReplacementToken;

        private readonly HashSet<SyntaxNode> _nodeSet;
        private readonly HashSet<SyntaxToken> _tokenSet;
        private readonly HashSet<TextSpan> _spanSet;

        private TextSpan _totalSpan;

        public Replacer(
            IEnumerable<TNode>? nodes = null,
            Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
            IEnumerable<SyntaxToken>? tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
        {
            _computeReplacementNode = computeReplacementNode;
            _computeReplacementToken = computeReplacementToken;

            _nodeSet = nodes != null ? [.. nodes] : s_noNodes;
            _tokenSet = tokens != null ? [.. tokens] : s_noTokens;

            _spanSet = [];
            CalculateVisitationCriteria();
        }

        public bool HasWork => _nodeSet.Count + _tokenSet.Count > 0;

        private void CalculateVisitationCriteria()
        {
            _spanSet.Clear();
            foreach (var node in _nodeSet)
            {
                _spanSet.Add(node.Span);
            }

            foreach (var token in _tokenSet)
            {
                _spanSet.Add(token.Span);
            }

            var first = true;
            var start = 0;
            var end = 0;

            foreach (var span in _spanSet)
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

            _totalSpan = new TextSpan(start, end - start);
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
                if (span.IntersectsWith(s))
                {
                    // node's full span intersects with at least one node to be replaced
                    // so we need to visit node's children to find it.
                    return true;
                }
            }

            return false;
        }

        [return: NotNullIfNotNull(nameof(node))]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            var rewritten = node;

            if (node != null)
            {
                var isReplacedNode = _nodeSet.Remove(node);

                if (isReplacedNode)
                {
                    // If node is in _nodeSet, then it contributed to the calculation of _spanSet.
                    // We are currently processing that node, so it no longer needs to contribute
                    // to _spanSet and affect determination of inward visitation. This is done before
                    // calling ShouldVisit to avoid walking into the node if there aren't any remaining
                    // spans inside it representing items to replace.
                    CalculateVisitationCriteria();
                }

                if (ShouldVisit(node.Span))
                {
                    rewritten = base.Visit(node);
                }

                if (isReplacedNode && _computeReplacementNode != null)
                {
                    rewritten = _computeReplacementNode((TNode)node, (TNode)rewritten!);
                }
            }

            return rewritten;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            var rewritten = token;
            var isReplacedToken = _tokenSet.Remove(token);

            if (isReplacedToken)
            {
                // If token is in _tokenSet, then it contributed to the calculation of _spanSet.
                // We are currently processing that token, so it no longer needs to contribute
                // to _spanSet and affect determination of inward visitation. This is done before
                // calling ShouldVisit to avoid walking into the token if there aren't any remaining
                // spans inside it representing items to replace.
                CalculateVisitationCriteria();
            }

            if (isReplacedToken && _computeReplacementToken != null)
            {
                rewritten = _computeReplacementToken(token, rewritten);
            }

            return rewritten;
        }
    }

    internal static SyntaxNode ReplaceNodeInList(SyntaxNode root, SyntaxNode originalNode, IEnumerable<SyntaxNode> newNodes)
    {
        return new NodeListEditor(originalNode, newNodes, ListEditKind.Replace).Visit(root);
    }

    internal static SyntaxNode InsertNodeInList(SyntaxNode root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
    {
        return new NodeListEditor(nodeInList, nodesToInsert, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
    }

    public static SyntaxNode ReplaceTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
    {
        return new TokenListEditor(tokenInList, newTokens, ListEditKind.Replace).Visit(root);
    }

    public static SyntaxNode InsertTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
    {
        return new TokenListEditor(tokenInList, newTokens, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
    }

    private enum ListEditKind
    {
        InsertBefore,
        InsertAfter,
        Replace
    }

    private abstract class BaseListEditor : SyntaxRewriter
    {
        private readonly TextSpan _elementSpan;

        protected readonly ListEditKind EditKind;

        protected BaseListEditor(TextSpan elementSpan, ListEditKind editKind)
        {
            _elementSpan = elementSpan;
            EditKind = editKind;
        }

        private bool ShouldVisit(TextSpan span)
        {
            if (span.IntersectsWith(_elementSpan))
            {
                // node's full span intersects with at least one node to be replaced
                // so we need to visit node's children to find it.
                return true;
            }

            return false;
        }

        [return: NotNullIfNotNull(nameof(node))]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            SyntaxNode? rewritten = node;

            if (node != null)
            {
                if (ShouldVisit(node.Span))
                {
                    rewritten = base.Visit(node);
                }
            }

            return rewritten;
        }
    }

    private sealed class NodeListEditor : BaseListEditor
    {
        private readonly SyntaxNode _originalNode;
        private readonly IEnumerable<SyntaxNode> _newNodes;

        public NodeListEditor(
            SyntaxNode originalNode,
            IEnumerable<SyntaxNode> replacementNodes,
            ListEditKind editKind)
            : base(originalNode.Span, editKind)
        {
            _originalNode = originalNode;
            _newNodes = replacementNodes;
        }

        [return: NotNullIfNotNull(nameof(node))]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node == _originalNode)
            {
                throw new InvalidOperationException("Expecting a list");
            }

            return base.Visit(node);
        }

        public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
        {
            if (_originalNode is TNode)
            {
                var index = list.IndexOf((TNode)_originalNode);
                if (index >= 0 && index < list.Count)
                {
                    switch (EditKind)
                    {
                        case ListEditKind.Replace:
                            return list.ReplaceRange((TNode)_originalNode, _newNodes.Cast<TNode>());

                        case ListEditKind.InsertAfter:
                            return list.InsertRange(index + 1, _newNodes.Cast<TNode>());

                        case ListEditKind.InsertBefore:
                            return list.InsertRange(index, _newNodes.Cast<TNode>());
                    }
                }
            }

            return base.VisitList(list);
        }
    }

    private class TokenListEditor : BaseListEditor
    {
        private readonly SyntaxToken _originalToken;
        private readonly IEnumerable<SyntaxToken> _newTokens;

        public TokenListEditor(
            SyntaxToken originalToken,
            IEnumerable<SyntaxToken> newTokens,
            ListEditKind editKind)
            : base(originalToken.Span, editKind)
        {
            _originalToken = originalToken;
            _newTokens = newTokens;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token == _originalToken)
            {
                throw new InvalidOperationException("Expecting a list");
            }

            return base.VisitToken(token);
        }

        public override SyntaxTokenList VisitList(SyntaxTokenList list)
        {
            var index = list.IndexOf(_originalToken);
            if (index >= 0 && index < list.Count)
            {
                switch (EditKind)
                {
                    case ListEditKind.Replace:
                        return list.ReplaceRange(_originalToken, _newTokens);

                    case ListEditKind.InsertAfter:
                        return list.InsertRange(index + 1, _newTokens);

                    case ListEditKind.InsertBefore:
                        return list.InsertRange(index, _newTokens);
                }
            }

            return base.VisitList(list);
        }
    }
}
