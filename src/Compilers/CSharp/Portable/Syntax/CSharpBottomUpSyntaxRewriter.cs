// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A rewriter the visits children before visiting parents.
    /// </summary>
    public abstract partial class CSharpBottomUpSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode>
    {
        private static ObjectPool<List<RewriteItem>> s_listPool 
            = new ObjectPool<List<RewriteItem>>(() => new List<RewriteItem>(20), 30);

        private readonly ChildReader _childReader;
        private readonly ChildVisitor _childVisitor;
        private readonly DefaultRewriter _defaultRewriter;
        private List<RewriteItem> _stack;

        private readonly bool _visitIntoStructuredTrivia;

        public virtual bool VisitIntoStructuredTrivia
        {
            get { return _visitIntoStructuredTrivia; }
        }

        protected CSharpBottomUpSyntaxRewriter(bool visitIntoStructuredTrivia = false)
        {
            _childReader = new ChildReader();
            _defaultRewriter = new DefaultRewriter(this);
            _childVisitor = new ChildVisitor(this);
            _visitIntoStructuredTrivia = visitIntoStructuredTrivia;
        }

        // block overrides: not meaningful since this is only called on some of the nodes, use VisitNode(original, rewritten) instead.
        [Obsolete("Use RewriteNode to initiate rewritting.")]
        public new SyntaxNode Visit(SyntaxNode node)
        {
            return node;
        }

        /// <summary>
        /// Rewrites the node by visiting the children (including all descendants) first and then visiting the node.
        /// </summary>
        public SyntaxNode RewriteNode(SyntaxNode node)
        { 
            // can be reentered during rewrite
            bool allocatedStack = false;
            if (_stack == null)
            {
                _stack = s_listPool.Allocate();
                allocatedStack = true;
            }

            try
            {
                if (!this.CanVisit(node))
                {
                    return node;
                }

                var rewritten = RewriteChildren(node);
                return this.VisitNode(node, rewritten);
            }
            finally
            {
                if (allocatedStack)
                {
                    if (_stack.Count < 100)
                    {
                        _stack.Clear();
                        s_listPool.Free(_stack);
                    }
                    else
                    {
                        s_listPool.ForgetTrackedObject(_stack);
                    }

                    _stack = null;
                }
            }
        }

        private SyntaxNode RewriteChildren(SyntaxNode node)
        {
            var initialCount = _stack.Count;
            _stack.Add(new RewriteItem(node, -1));

            while (true)
            {
                var itemIndex = _stack.Count - 1;
                var item = _stack[itemIndex];

                if (item.MoveNext())
                {
                    _stack[itemIndex] = item;
                    var child = _childReader.ReadChild(item.Original, item.ChildIndex);

                    if (child.Kind == ChildKind.Node)
                    {
                        if (child.Node != null && this.CanVisit(child.Node))
                        {
                            _stack.Add(new RewriteItem(child.Node, item.ChildIndex));
                            continue;
                        }
                    }
                    else
                    {
                        item.Updated = _childVisitor.VisitChild(item.Updated, item.ChildIndex, child);
                        _stack[itemIndex] = item;
                    }
                }
                else
                {
                    // pop
                    _stack[itemIndex] = default(RewriteItem);
                    _stack.RemoveAt(itemIndex);

                    if (_stack.Count > initialCount && item.IndexInParent >= 0)
                    {
                        var parentIndex = _stack.Count - 1;
                        var parent = _stack[parentIndex];
                        parent.Updated = _childVisitor.VisitChild(parent.Updated, item.IndexInParent, new RewriteChild(item.Original), new RewriteChild(item.Updated));
                        _stack[parentIndex] = parent;
                    }
                    else
                    {
                        return item.Updated;
                    }
                }
            }
        }

        /// <summary>
        /// Rewrites the token by visiting all leading and trailing trivia first and then visiting the token.
        /// </summary>
        public SyntaxToken RewriteToken(SyntaxToken token)
        {
            return _defaultRewriter.VisitToken(token);
        }

        /// <summary>
        /// Rewrites the trivia by visiting any structure first and then visiting the trivia.
        /// </summary>
        public SyntaxTrivia RewriteTrivia(SyntaxTrivia trivia)
        {
            return _defaultRewriter.VisitTrivia(trivia);
        }

        public override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            return node;
        }

        /// <summary>
        /// Determines whether the node and any of it descendants will be visisted.
        /// </summary>
        public virtual bool CanVisit(SyntaxNode node)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the token and any of its leading and trailing trivia will be visited.
        /// </summary>
        public virtual bool CanVisit(SyntaxToken token)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the trivia and its structure will be visited.
        /// </summary>
        public virtual bool CanVisit(SyntaxTrivia trivia)
        {
            return true;
        }

        /// <summary>
        /// Called after all child nodes and tokens have been visited.
        /// </summary>
        /// <param name="original">The node found in the original tree.</param>
        /// <param name="rewritten">The node with child nodes and tokens potentially updated.</param>
        public virtual SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
        {
            // route to type VisitXXX from syntax visitor base
            return base.Visit(rewritten); 
        }

        /// <summary>
        /// Called after all leading and trailing trivia have been visited.
        /// </summary>
        /// <param name="original">The token found in the original tree.</param>
        /// <param name="rewritten">The token with leading and trailing trivia potentially updated.</param>
        public virtual SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
        {
            return rewritten;
        }

        /// <summary>
        /// Called after any sub structure has been visited.
        /// </summary>
        /// <param name="original">The trivia found in the original tree.</param>
        /// <param name="rewritten">The trivia with the sub structure potentially updated.</param>
        /// <returns></returns>
        public virtual SyntaxTrivia VisitTrivia(SyntaxTrivia original, SyntaxTrivia rewritten)
        {
            return rewritten;
        }

        /// <summary>
        /// Called after all list elements have been visited.
        /// </summary>
        /// <param name="original">The list found in the original tree.</param>
        /// <param name="rewritten">The list with elements potentially updated.</param>
        public virtual SyntaxTokenList VisitList(SyntaxTokenList original, SyntaxTokenList rewritten)
        {
            return rewritten;
        }

        /// <summary>
        /// Called after all list elements have been visited.
        /// </summary>
        /// <param name="original">The list found in the original tree.</param>
        /// <param name="rewritten">The list with elements potentially updated.</param>
        public virtual SyntaxTriviaList VisitList(SyntaxTriviaList original, SyntaxTriviaList rewritten)
        {
            return rewritten;
        }

        /// <summary>
        /// Called after all list elements have been visited.
        /// </summary>
        /// <param name="original">The list found in the original tree.</param>
        /// <param name="rewritten">The list with elements potentially updated.</param>
        public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> original, SyntaxList<TNode> rewritten) where TNode : SyntaxNode
        {
            return rewritten;
        }

        /// <summary>
        /// Called after all list elements and separators have been visited.
        /// </summary>
        /// <param name="original">The list found in the original tree.</param>
        /// <param name="rewritten">The list with elements and separators potentially updated.</param>
        public virtual SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> original, SeparatedSyntaxList<TNode> rewritten) where TNode : SyntaxNode
        {
            return rewritten;
        }

        /// <summary>
        /// Called when the element node of a list is being visited. The default behavior is to call <see cref="VisitNode"/>.
        /// </summary>
        /// <param name="original">The element node found in the original tree.</param>
        /// <param name="rewritten">The element node with children potentially updated.</param>
        public virtual SyntaxNode VisitListElement(SyntaxNode original, SyntaxNode rewritten)
        {
            return this.VisitNode(original, rewritten);
        }

        /// <summary>
        /// Called when the element token of a list is being visited. The default behavior is to call <see cref="VisitToken"/>.
        /// </summary>
        /// <param name="original">The element token found in the original tree.</param>
        /// <param name="rewritten">The element token with leading and trailing trivia potentially updated.</param>
        public virtual SyntaxToken VisitListElement(SyntaxToken original, SyntaxToken rewritten)
        {
            return this.VisitToken(original, rewritten);
        }

        /// <summary>
        /// Called when the element trivia of a list is being visited. The default behavior is to call <see cref="VisitTrivia"/>.
        /// </summary>
        /// <param name="original">The element trivia found in the original tree.</param>
        /// <param name="rewritten">The element trivia with the sub structure potentially updated.</param>
        public virtual SyntaxTrivia VisitListElement(SyntaxTrivia original, SyntaxTrivia rewritten)
        {
            return this.VisitTrivia(original, rewritten);
        }

        /// <summary>
        /// Called when the separator token of a list is being visited. The default behavior is to call <see cref="VisitToken"/>.
        /// </summary>
        /// <param name="original">The separator token found in the original tree.</param>
        /// <param name="rewritten">The separator token with leading and trailing trivia potentially updated.</param>
        public virtual SyntaxToken VisitListSeparator(SyntaxToken original, SyntaxToken rewritten)
        {
            return this.VisitToken(original, rewritten);
        }

        private struct RewriteItem
        {
            public int IndexInParent;
            public SyntaxNode Original;
            public int ChildIndex;

            public RewriteItem(SyntaxNode node, int indexInParent)
            {
                this.Original = node;
                this._updated = node;
                this.IndexInParent = indexInParent;
                this.ChildIndex = -1;
            }

            public bool MoveNext()
            {
                this.ChildIndex++;
                return this.ChildIndex < this.Original.SlotCount;
            }

            private SyntaxNode _updated;
            public SyntaxNode Updated
            {
                get { return _updated; }
                set
                {
                    if (value != _updated)
                    {
                        _updated = value;
                    }
                }
            }

        }

        private enum ChildKind
        {
            Unknown,
            Node,
            Token,
            List
        }

        /// <summary>
        /// A union that represents a child of a node
        /// </summary>
        private struct RewriteChild
        {
            public readonly ChildKind Kind;
            public readonly SyntaxNode Node;
            public readonly SyntaxToken Token;
            public readonly SyntaxNodeOrTokenList List;
            public readonly SyntaxTokenList TokenList;

            private RewriteChild(SyntaxNode node, ChildKind kind)
            {
                this.Kind = kind;
                this.Node = node;
                this.Token = default(SyntaxToken);
                this.List = default(SyntaxNodeOrTokenList);
                this.TokenList = default(SyntaxTokenList);
            }

            public RewriteChild(SyntaxNode node)
                : this(node, ChildKind.Node)
            {
            }

            public RewriteChild(SyntaxToken token)
            {
                this.Kind = ChildKind.Token;
                this.Node = null;
                this.Token = token;
                this.List = default(SyntaxNodeOrTokenList);
                this.TokenList = default(SyntaxTokenList);
            }

            public RewriteChild(SyntaxTokenList list)
            {
                this.Kind = ChildKind.List;
                this.Node = null;
                this.Token = default(SyntaxToken);
                this.List = default(SyntaxNodeOrTokenList);
                this.TokenList = list;
            }

            public RewriteChild(SyntaxNodeOrTokenList list)
            {
                this.Kind = ChildKind.List;
                this.Node = null;
                this.Token = default(SyntaxToken);
                this.List = list;
                this.TokenList = default(SyntaxTokenList);
            }

            public static RewriteChild From(SyntaxTokenList list)
            {
                return new RewriteChild(list);
            }

            public static RewriteChild From<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
            {
                return new RewriteChild(list.Node, ChildKind.List);
            }

            public static RewriteChild From<TNode>(SeparatedSyntaxList<TNode> list) where TNode : SyntaxNode
            {
                return new RewriteChild(list.GetWithSeparators());
            }

            public SyntaxList<TNode> AsList<TNode>() where TNode : SyntaxNode
            {
                return new SyntaxList<TNode>(this.Node);
            }

            public SeparatedSyntaxList<TNode> AsSeparatedList<TNode>() where TNode : SyntaxNode
            {
                return new SeparatedSyntaxList<TNode>(this.List);
            }
        }

        /// <summary>
        /// A  rewriter used to visit & rewriter individual children of a node.
        /// </summary>
        private class ChildVisitor : CSharpSyntaxRewriter
        {
            private readonly CSharpBottomUpSyntaxRewriter _rewriter;
            private readonly DefaultRewriter _defaultRewriter;
            private RewriteChild _originalChild;
            private RewriteChild _updatedChild;
            private int _childIndex;
            private int _index;

            public ChildVisitor(CSharpBottomUpSyntaxRewriter rewriter)
            {
                _rewriter = rewriter;
                _defaultRewriter = _rewriter._defaultRewriter;
            }

            public override bool VisitIntoStructuredTrivia
            {
                get { return _rewriter.VisitIntoStructuredTrivia; }
            }

            public SyntaxNode VisitChild(SyntaxNode node, int childIndex, RewriteChild originalChild, RewriteChild updatedChild = default(RewriteChild))
            {
                // visitor may get reentered.
                var saveChildIndex = _childIndex;
                var saveOriginalChild = _originalChild;
                var saveUpdatedChild = _updatedChild;
                var saveIndex = _index;

                _childIndex = childIndex;
                _originalChild = originalChild;
                _updatedChild = updatedChild;
                _index = -1;
                var result = base.Visit(node);

                _originalChild = saveOriginalChild;
                _updatedChild = saveUpdatedChild;
                _childIndex = saveChildIndex;
                _index = saveIndex;

                return result;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                _index++;

                if (_index == _childIndex)
                {
                    // note: don't default visit nodes, since they are handled by stack
                    if (_updatedChild.Kind == ChildKind.Unknown)
                    {
                        return _rewriter.VisitNode(_originalChild.Node, node);
                    }
                    else
                    {
                        return _rewriter.VisitNode(_originalChild.Node, _updatedChild.Node);
                    }
                }

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                _index++;

                if (_index == _childIndex && _rewriter.CanVisit(_originalChild.Token))
                {
                    var originalToken = _originalChild.Token;
                    var rewrittenLeading = _rewriter.VisitList(originalToken.LeadingTrivia, _defaultRewriter.VisitList(originalToken.LeadingTrivia));
                    var rewrittenTrailing = _rewriter.VisitList(originalToken.TrailingTrivia, _defaultRewriter.VisitList(originalToken.TrailingTrivia));

                    var rewrittenToken = originalToken;
                    if (rewrittenLeading != originalToken.LeadingTrivia)
                    {
                        rewrittenToken = rewrittenToken.WithLeadingTrivia(rewrittenLeading);
                    }

                    if (rewrittenTrailing != originalToken.TrailingTrivia)
                    {
                        rewrittenToken = rewrittenToken.WithTrailingTrivia(rewrittenTrailing);
                    }

                    return _rewriter.VisitToken(_originalChild.Token, rewrittenToken);
                }

                return token;
            }

            public override SyntaxTokenList VisitList(SyntaxTokenList list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    var newList = _defaultRewriter.VisitList(_originalChild.TokenList);
                    return _rewriter.VisitList(_originalChild.TokenList, newList);
                }

                return list;
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    var original = _originalChild.AsList<TNode>();
                    var newList = _defaultRewriter.VisitList(original);
                    return _rewriter.VisitList(original, newList);
                }

                return list;
            }

            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    var original = _originalChild.AsSeparatedList<TNode>();
                    var newList = _defaultRewriter.VisitList(original);
                    return _rewriter.VisitList(original, newList);
                }

                return list;
            }
        }

        /// <summary>
        /// A syntax rewriter used to invoke recursive rewriter behavior.
        /// </summary>
        private class DefaultRewriter : CSharpSyntaxRewriter
        {
            private readonly CSharpBottomUpSyntaxRewriter _rewriter;

            public DefaultRewriter(CSharpBottomUpSyntaxRewriter rewriter)
            {
                _rewriter = rewriter;
            }

            public override bool VisitIntoStructuredTrivia
            {
                get { return _rewriter.VisitIntoStructuredTrivia; }
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null && _rewriter.CanVisit(node))
                {
                    // reentrant (recursion) should get trigged on list elements and structured trivia roots
                    var rewritten = _rewriter.RewriteChildren(node);
                    return _rewriter.VisitNode(node, rewritten);
                }
                else
                {
                    return node;
                }
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_rewriter.CanVisit(token))
                {
                    var newToken = base.VisitToken(token);
                    return _rewriter.VisitToken(token, newToken);
                }
                else
                {
                    return token;
                }
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (_rewriter.CanVisit(trivia))
                {
                    var newTrivia = base.VisitTrivia(trivia);
                    return _rewriter.VisitTrivia(trivia, newTrivia);
                }
                else
                {
                    return trivia;
                }
            }

            public override TNode VisitListElement<TNode>(TNode node)
            {
                if (_rewriter.CanVisit(node))
                {
                    // call RewriteChildren to only rewrite children & not invoke VisitNode
                    var rewritten = _rewriter.RewriteChildren(node);
                    return (TNode)_rewriter.VisitListElement(node, rewritten);
                }
                else
                {
                    return node;
                }
            }

            public override SyntaxToken VisitListSeparator(SyntaxToken separator)
            {
                if (_rewriter.CanVisit(separator))
                {
                    // call base.VisitToken to only rewrite trivia & not invoke VisitToken
                    var rewritten = base.VisitToken(separator);
                    return _rewriter.VisitListSeparator(separator, rewritten);
                }
                else
                {
                    return separator;
                }
            }

            public override SyntaxToken VisitListElement(SyntaxToken element)
            {
                if (_rewriter.CanVisit(element))
                {
                    // call base.VisitToken to only rewrite trivia & not invoke VisitToken
                    var rewritten = base.VisitToken(element);
                    return _rewriter.VisitListElement(element, rewritten);
                }
                else
                {
                    return element;
                }
            }

            public override SyntaxTrivia VisitListElement(SyntaxTrivia element)
            {
                if (_rewriter.CanVisit(element))
                {
                    // call base.VisitTrivia to only rewrite nested structure & not invoke VisitTrivia
                    var rewritten = base.VisitTrivia(element);
                    return _rewriter.VisitListElement(element, rewritten);
                }
                else
                {
                    return element;
                }
            }
        }

        /// <summary>
        /// A rewriter used to access individual children of a node.
        /// </summary>
        private class ChildReader : CSharpSyntaxRewriter
        {
            private RewriteChild _child;
            private int _childIndex;
            private int _index;

            public RewriteChild ReadChild(SyntaxNode node, int childIndex)
            {
                _child = default(RewriteChild);
                _childIndex = childIndex;
                _index = -1;
                base.Visit(node);
                return _child;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                _index++;

                if (_index == _childIndex)
                {
                    _child = new RewriteChild(node);
                }

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                _index++;

                if (_index == _childIndex)
                {
                    _child = new RewriteChild(token);
                }

                return token;
            }

            public override SyntaxTokenList VisitList(SyntaxTokenList list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    _child = new RewriteChild(list);
                }

                return list;
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    _child = RewriteChild.From(list);
                }

                return list;
            }

            public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list)
            {
                _index++;

                if (_index == _childIndex)
                {
                    _child = RewriteChild.From(list);
                }

                return list;
            }
        }
    }
}