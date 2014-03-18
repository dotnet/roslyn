// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                return replacer.Visit(root);
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
                return replacer.VisitToken(root);
            }
            else
            {
                return root;
            }
        }

        private class Replacer<TNode> : CSharpSyntaxRewriter where TNode : SyntaxNode
        {
            private readonly Func<TNode, TNode, SyntaxNode> computeReplacementNode;
            private readonly Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken;
            private readonly Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia;

            private readonly HashSet<SyntaxNode> nodeSet;
            private readonly HashSet<SyntaxToken> tokenSet;
            private readonly HashSet<SyntaxTrivia> triviaSet;
            private readonly HashSet<TextSpan> spanSet;

            private readonly TextSpan totalSpan;
            private readonly bool visitIntoStructuredTrivia;
            private readonly bool shouldVisitTrivia;

            public Replacer(
                IEnumerable<TNode> nodes,
                Func<TNode, TNode, SyntaxNode> computeReplacementNode,
                IEnumerable<SyntaxToken> tokens,
                Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken,
                IEnumerable<SyntaxTrivia> trivia,
                Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            {
                this.computeReplacementNode = computeReplacementNode;
                this.computeReplacementToken = computeReplacementToken;
                this.computeReplacementTrivia = computeReplacementTrivia;

                this.nodeSet = nodes != null ? new HashSet<SyntaxNode>(nodes) : NoNodes;
                this.tokenSet = tokens != null ? new HashSet<SyntaxToken>(tokens) : NoTokens;
                this.triviaSet = trivia != null ? new HashSet<SyntaxTrivia>(trivia) : NoTrivia;

                this.spanSet = new HashSet<TextSpan>(
                    this.nodeSet.Select(n => n.FullSpan).Concat(
                    this.tokenSet.Select(t => t.FullSpan).Concat(
                    this.triviaSet.Select(t => t.FullSpan))));

                this.totalSpan = ComputeTotalSpan(this.spanSet);

                this.visitIntoStructuredTrivia =
                    this.nodeSet.Any(n => n.IsPartOfStructuredTrivia()) ||
                    this.tokenSet.Any(t => t.IsPartOfStructuredTrivia()) ||
                    this.triviaSet.Any(t => t.IsPartOfStructuredTrivia());

                this.shouldVisitTrivia = this.triviaSet.Count > 0 || this.visitIntoStructuredTrivia;
            }

            private static readonly HashSet<SyntaxNode> NoNodes = new HashSet<SyntaxNode>();
            private static readonly HashSet<SyntaxToken> NoTokens = new HashSet<SyntaxToken>();
            private static readonly HashSet<SyntaxTrivia> NoTrivia = new HashSet<SyntaxTrivia>();

            public override bool VisitIntoStructuredTrivia
            {
                get
                {
                    return this.visitIntoStructuredTrivia;
                }
            }

            public bool HasWork
            {
                get
                {
                    return this.nodeSet.Count + this.tokenSet.Count + this.triviaSet.Count > 0;
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
                if (!span.IntersectsWith(this.totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var s in this.spanSet)
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

            public override SyntaxNode Visit(SyntaxNode node)
            {
                SyntaxNode rewritten = node;

                if (node != null)
                {
                    if (this.ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }

                    if (this.nodeSet.Contains(node) && this.computeReplacementNode != null)
                    {
                        rewritten = this.computeReplacementNode((TNode)node, (TNode)rewritten);
                    }
                }

                return rewritten;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var rewritten = token;

                if (this.shouldVisitTrivia && this.ShouldVisit(token.FullSpan))
                {
                    rewritten = base.VisitToken(token);
                }

                if (this.tokenSet.Contains(token) && this.computeReplacementToken != null)
                {
                    rewritten = this.computeReplacementToken(token, rewritten);
                }

                return rewritten;
            }

            public override SyntaxTrivia VisitListElement(SyntaxTrivia trivia)
            {
                var rewritten = trivia;

                if (this.VisitIntoStructuredTrivia && trivia.HasStructure && this.ShouldVisit(trivia.FullSpan))
                {
                    rewritten = this.VisitTrivia(trivia);
                }

                if (this.triviaSet.Contains(trivia) && this.computeReplacementTrivia != null)
                {
                    rewritten = this.computeReplacementTrivia(trivia, rewritten);
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

        public static SyntaxNode ReplaceTriviaInList(SyntaxNode root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia)
        {
            return new TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).Visit(root);
        }

        public static SyntaxNode InsertTriviaInList(SyntaxNode root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
        {
            return new TriviaListEditor(triviaInList, newTrivia, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
        }

        public static SyntaxToken ReplaceTriviaInList(SyntaxToken root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia)
        {
            return new TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).VisitToken(root);
        }

        public static SyntaxToken InsertTriviaInList(SyntaxToken root, SyntaxTrivia triviaInList, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
        {
            return new TriviaListEditor(triviaInList, newTrivia, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).VisitToken(root);
        }

        private enum ListEditKind
        {
            InsertBefore,
            InsertAfter,
            Replace
        }

        private static InvalidOperationException GetItemNotListElementException()
        {
            return new InvalidOperationException("The item specified is not the element of a list.".NeedsLocalization());
        }

        private abstract class BaseListEditor : CSharpSyntaxRewriter
        {
            private readonly TextSpan elementSpan;
            private readonly bool visitTrivia;
            private readonly bool visitIntoStructuredTrivia;

            protected readonly ListEditKind editKind;

            public BaseListEditor(
                TextSpan elementSpan,
                ListEditKind editKind,
                bool visitTrivia,
                bool visitIntoStructuredTrivia)
            {
                this.elementSpan = elementSpan;
                this.editKind = editKind;
                this.visitTrivia = visitTrivia || visitIntoStructuredTrivia;
                this.visitIntoStructuredTrivia = visitIntoStructuredTrivia;
            }

            public override bool VisitIntoStructuredTrivia
            {
                get
                {
                    return this.visitIntoStructuredTrivia;
                }
            }

            private bool ShouldVisit(TextSpan span)
            {
                if (span.IntersectsWith(this.elementSpan))
                {
                    // node's full span intersects with at least one node to be replaced
                    // so we need to visit node's children to find it.
                    return true;
                }

                return false;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                SyntaxNode rewritten = node;

                if (node != null)
                {
                    if (this.ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }
                }

                return rewritten;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var rewritten = token;

                if (this.visitTrivia && this.ShouldVisit(token.FullSpan))
                {
                    rewritten = base.VisitToken(token);
                }

                return rewritten;
            }

            public override SyntaxTrivia VisitListElement(SyntaxTrivia trivia)
            {
                var rewritten = trivia;

                if (this.VisitIntoStructuredTrivia && trivia.HasStructure && this.ShouldVisit(trivia.FullSpan))
                {
                    rewritten = this.VisitTrivia(trivia);
                }

                return rewritten;
            }
        }

        private class NodeListEditor : BaseListEditor
        {
            private readonly SyntaxNode originalNode;
            private readonly IEnumerable<SyntaxNode> newNodes;

            public NodeListEditor(
                SyntaxNode originalNode,
                IEnumerable<SyntaxNode> replacementNodes,
                ListEditKind editKind)
                : base(originalNode.Span, editKind, false, originalNode.IsPartOfStructuredTrivia())
            {
                this.originalNode = originalNode;
                this.newNodes = replacementNodes;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == originalNode)
                {
                    throw GetItemNotListElementException();
                }

                return base.Visit(node);
            }

            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list)
            {
                if (this.originalNode is TNode)
                {
                    var index = list.IndexOf((TNode)this.originalNode);
                    if (index >= 0 && index < list.Count)
                    {
                        switch (this.editKind)
                        {
                            case ListEditKind.Replace:
                                return list.ReplaceRange((TNode)this.originalNode, this.newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return list.InsertRange(index + 1, this.newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return list.InsertRange(index, this.newNodes.Cast<TNode>());
                        }
                    }
                }

                return base.VisitList<TNode>(list);
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
            {
                if (this.originalNode is TNode)
                {
                    var index = list.IndexOf((TNode)this.originalNode);
                    if (index >= 0 && index < list.Count)
                    {
                        switch (this.editKind)
                        {
                            case ListEditKind.Replace:
                                return list.ReplaceRange((TNode)this.originalNode, this.newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return list.InsertRange(index + 1, this.newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return list.InsertRange(index, this.newNodes.Cast<TNode>());
                        }
                    }
                }

                return base.VisitList<TNode>(list);
            }
        }

        private class TokenListEditor : BaseListEditor
        {
            private readonly SyntaxToken originalToken;
            private readonly IEnumerable<SyntaxToken> newTokens;

            public TokenListEditor(
                SyntaxToken originalToken,
                IEnumerable<SyntaxToken> newTokens,
                ListEditKind editKind)
                : base(originalToken.Span, editKind, false, originalToken.IsPartOfStructuredTrivia())
            {
                this.originalToken = originalToken;
                this.newTokens = newTokens;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token == originalToken)
                {
                    throw GetItemNotListElementException();
                }

                return base.VisitToken(token);
            }

            public override SyntaxTokenList VisitList(SyntaxTokenList list)
            {
                var index = list.IndexOf(this.originalToken);
                if (index >= 0 && index < list.Count)
                {
                    switch (this.editKind)
                    {
                        case ListEditKind.Replace:
                            return list.ReplaceRange(this.originalToken, this.newTokens);

                        case ListEditKind.InsertAfter:
                            return list.InsertRange(index + 1, this.newTokens);

                        case ListEditKind.InsertBefore:
                            return list.InsertRange(index, this.newTokens);
                    }
                }

                return base.VisitList(list);
            }
        }

        private class TriviaListEditor : BaseListEditor
        {
            private readonly SyntaxTrivia originalTrivia;
            private readonly IEnumerable<SyntaxTrivia> newTrivia;

            public TriviaListEditor(
                SyntaxTrivia originalTrivia,
                IEnumerable<SyntaxTrivia> newTrivia,
                ListEditKind editKind)
                : base(originalTrivia.Span, editKind, true, originalTrivia.IsPartOfStructuredTrivia())
            {
                this.originalTrivia = originalTrivia;
                this.newTrivia = newTrivia;
            }

            public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            {
                var index = list.IndexOf(this.originalTrivia);
                if (index >= 0 && index < list.Count)
                {
                    switch (this.editKind)
                    {
                        case ListEditKind.Replace:
                            return list.ReplaceRange(this.originalTrivia, this.newTrivia);

                        case ListEditKind.InsertAfter:
                            return list.InsertRange(index + 1, this.newTrivia);

                        case ListEditKind.InsertBefore:
                            return list.InsertRange(index, this.newTrivia);
                    }
                }

                return base.VisitList(list);
            }
        }
    }
}
