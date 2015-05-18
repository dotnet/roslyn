// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public abstract partial class CSharpBottomUpSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode>
    {
        private static ObjectPool<List<RewriteItem>> s_listPool 
            = new ObjectPool<List<RewriteItem>>(() => new List<RewriteItem>(20), 30);

        private readonly ChildReader _childReader;
        private readonly ChildVisitor _childVisitor;
        private List<RewriteItem> _stack;

        protected CSharpBottomUpSyntaxRewriter(bool visitIntoStructuredTrivia = false)
        {
            _childReader = new ChildReader();
            _childVisitor = new ChildVisitor(this, visitIntoStructuredTrivia);
        }

        // block overrides: not meaningful since this is only called on some of the nodes, use VisitNode(original, rewritten) instead.
        public new SyntaxNode Visit(SyntaxNode node)
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
                            if (this.CanVisit(child.Node))
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

                        if (_stack.Count > 0 && item.IndexInParent >= 0)
                        {
                            var parentIndex = _stack.Count - 1;
                            var parent = _stack[parentIndex];
                            parent.Updated = _childVisitor.VisitChild(parent.Updated, item.IndexInParent, new RewriteChild(item.Original), new RewriteChild(item.Updated));
                            _stack[parentIndex] = parent;
                        }
                        else
                        {
                            return this.VisitNode(item.Original, item.Updated);
                        }
                    }
                }
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

        public override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            return node;
        }

        public virtual bool CanVisit(SyntaxNode node)
        {
            return true;
        }

        public virtual bool CanVisit(SyntaxToken token)
        {
            return true;
        }

        public virtual bool CanVisit(SyntaxTrivia trivia)
        {
            return true;
        }

        public virtual SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
        {
            // route to type VisitXXX from syntax visitor base
            return base.Visit(rewritten); 
        }

        public virtual SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
        {
            return rewritten;
        }

        public virtual SyntaxTrivia VisitTrivia(SyntaxTrivia original, SyntaxTrivia rewritten)
        {
            return rewritten;
        }

        public virtual SyntaxTokenList VisitList(SyntaxTokenList original, SyntaxTokenList rewritten)
        {
            return rewritten;
        }

        public virtual SyntaxTriviaList VisitList(SyntaxTriviaList original, SyntaxTriviaList rewritten)
        {
            return rewritten;
        }

        public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> original, SyntaxList<TNode> rewritten) where TNode : SyntaxNode
        {
            return rewritten;
        }

        public virtual SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> original, SeparatedSyntaxList<TNode> rewritten) where TNode : SyntaxNode
        {
            return rewritten;
        }

        private struct RewriteItem
        {
            public int IndexInParent;
            public SyntaxNode Original;
            public SyntaxNode Updated;
            public int ChildIndex;

            public RewriteItem(SyntaxNode node, int indexInParent)
            {
                this.Original = node;
                this.Updated = node;
                this.IndexInParent = indexInParent;
                this.ChildIndex = -1;
            }

            public bool MoveNext()
            {
                this.ChildIndex++;
                return this.ChildIndex < this.Original.SlotCount;
            }
        }

        private enum ChildKind
        {
            Unknown,
            Node,
            Token,
            List
        }

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

        private class ChildVisitor : CSharpSyntaxRewriter
        {
            private readonly CSharpBottomUpSyntaxRewriter _rewriter;
            private readonly DefaultRewriter _defaultRewriter;
            private RewriteChild _originalChild;
            private RewriteChild _updatedChild;
            private int _childIndex;
            private int _index;

            public ChildVisitor(CSharpBottomUpSyntaxRewriter rewriter, bool visitIntoStructuredTrivia)
            {
                _rewriter = rewriter;
                _defaultRewriter = new DefaultRewriter(rewriter, visitIntoStructuredTrivia);
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
                    var newToken = _defaultRewriter.VisitToken(_originalChild.Token);
                    return _rewriter.VisitToken(_originalChild.Token, newToken);
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

        private class DefaultRewriter : CSharpSyntaxRewriter
        {
            private readonly CSharpBottomUpSyntaxRewriter _rewriter;

            public DefaultRewriter(CSharpBottomUpSyntaxRewriter rewriter, bool visitIntoStructuredTrivia)
                : base(visitIntoStructuredTrivia)
            {
                _rewriter = rewriter;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                // reentrant (recursion) should get trigged on list elements and structured trivia roots
                return _rewriter.Visit(node);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_rewriter.CanVisit(token))
                {
                    var newToken = base.VisitToken(token);
                    return _rewriter.VisitToken(token, newToken);
                }

                return token;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (_rewriter.CanVisit(trivia))
                {
                    var newTrivia = base.VisitTrivia(trivia);
                    return this._rewriter.VisitTrivia(trivia, newTrivia);
                }

                return trivia;
            }
        }

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