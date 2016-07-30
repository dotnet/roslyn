// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a non-recursive visitor which descends an entire <see cref="CSharpSyntaxNode"/> graph and
    /// may replace or remove visited SyntaxNodes in depth-first order.
    /// </summary>
    public class CSharpNonRecursiveSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode>
    {
        private readonly Stack<SyntaxNodeOrToken> _undeconstructedStack;
        private readonly Stack<UntransformedNode> _untransformedStack;
        private readonly Stack<SyntaxNodeOrToken> _transformedStack;
        private readonly NodeDeconstructor _deconstructor;
        private readonly NodeReassembler _reassembler;
        private readonly TriviaRewriter _trivializer;
        private readonly List<SyntaxNodeOrToken> _children;

        protected CSharpNonRecursiveSyntaxRewriter(bool visitIntoStructuredTrivia = false)
        {
            _undeconstructedStack = new Stack<SyntaxNodeOrToken>();
            _transformedStack = new Stack<SyntaxNodeOrToken>();
            _untransformedStack = new Stack<UntransformedNode>();
            _deconstructor = new NodeDeconstructor();
            _reassembler = new NodeReassembler();
            _trivializer = new TriviaRewriter(this, visitIntoStructuredTrivia);
            _children = new List<SyntaxNodeOrToken>();
        }

        protected SyntaxNode Original { get; private set; }

        protected virtual bool Skip(SyntaxNodeOrToken nodeOrToken, out SyntaxNodeOrToken rewriten)
        {
            rewriten = default(SyntaxNodeOrToken);
            return false;
        }

        public virtual SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
        {
            return ((CSharpSyntaxNode)rewritten).Accept(this);
        }

        public virtual SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
        {
            return rewritten;
        }

        public virtual SyntaxTrivia VisitTrivia(SyntaxTrivia original, SyntaxTrivia rewritten)
        {
            return rewritten;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            return this.Rewrite(node);
        }

        public override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            return node;
        }

        public SyntaxNode Rewrite(SyntaxNode node)
        {
            int undeconstructedStart = _undeconstructedStack.Count;
            int untransformedStart = _untransformedStack.Count;
            int transformedStart = _transformedStack.Count;

            // add initial node so we have something to work on
            _undeconstructedStack.Push(node);

            // as long as there is more to deconstruct, there is more work to do
            while (_undeconstructedStack.Count > undeconstructedStart)
            {
                var nodeOrToken = _undeconstructedStack.Pop();
                if (nodeOrToken.IsNode)
                {
                    node = nodeOrToken.AsNode();

                    if (node == null)
                    {
                        // nulls just stay nulls, they don't get transformed
                        _transformedStack.Push(nodeOrToken);
                    }
                    else
                    {
                        SyntaxNodeOrToken rewriten;
                        if (this.Skip(node, out rewriten))
                        {
                            _transformedStack.Push(rewriten);
                        }
                        else
                        {
                            // deconstruct node into child elements
                            _children.Clear();
                            _deconstructor.Deconstruct(node, _children);

                            // add child elements to undeconstructed stack in reverse order so
                            // the first child gets operated on next
                            for (int i = _children.Count - 1; i >= 0; i--)
                            {
                                _undeconstructedStack.Push(_children[i]);
                            }

                            // remember the node that will be tranformed later after the children are transformed
                            _untransformedStack.Push(new UntransformedNode(node, _children.Count, _transformedStack.Count));
                        }
                    }
                }
                else if (nodeOrToken.IsToken)
                {
                    // we can transform tokens immediately
                    var original = nodeOrToken.AsToken();
                    SyntaxNodeOrToken rewriten;
                    if (this.Skip(original, out rewriten))
                    {
                        _transformedStack.Push(rewriten);
                    }
                    else
                    {
                        var rewrittenToken = _trivializer.VisitToken(original); // rewrite trivia
                        var transformed = this.VisitToken(original, rewrittenToken);
                        _transformedStack.Push(transformed);
                    }
                }

                // transform any nodes that can be transformed now
                while (_untransformedStack.Count > untransformedStart
                    && _untransformedStack.Peek().HasAllChildrenOnStack(_transformedStack))
                {
                    var untransformed = _untransformedStack.Pop();

                    // gather transformed children for this node
                    _children.Clear();
                    for (int i = 0; i < untransformed.ChildCount; i++)
                    {
                        _children.Add(_transformedStack.Pop());
                    }

                    _children.Reverse();

                    // reassemble original node with tranformed children
                    var rewritten = _reassembler.Reassemble(untransformed.Node, _children);

                    // now tranform the node
                    var save = this.Original;
                    this.Original = untransformed.Node;
                    var transformed = this.VisitNode(untransformed.Node, rewritten.AsNode());
                    this.Original = save;

                    // add newly transformed node to the transformed stack
                    _transformedStack.Push(transformed);
                }
            }

            Debug.Assert(_untransformedStack.Count == untransformedStart);
            Debug.Assert(_transformedStack.Count == transformedStart + 1);

            return _transformedStack.Pop().AsNode();
        }

        private struct UntransformedNode
        {
            public SyntaxNode Node { get; }
            public int ChildCount { get; } // the number of children the original had, that we need to have 
            public int TransformedStackStart { get; } // the transformed stack top when the untransformed node was created

            public UntransformedNode(SyntaxNode node, int childCount, int transformedStackStart)
            {
                this.Node = node;
                this.ChildCount = childCount;
                this.TransformedStackStart = transformedStackStart;
            }

            public bool HasAllChildrenOnStack(Stack<SyntaxNodeOrToken> transformedStack)
            {
                return this.TransformedStackStart + this.ChildCount == transformedStack.Count;
            }
        }

        private class NodeDeconstructor : CSharpSyntaxRewriter
        {
            private List<SyntaxNodeOrToken> _elements;

            public void Deconstruct(SyntaxNode node, List<SyntaxNodeOrToken> elements)
            {
                _elements = elements;
                ((CSharpSyntaxNode)node).Accept(this);
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                _elements.Add(node);
                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                _elements.Add(token);
                return token;
            }
        }

        private class NodeReassembler : CSharpSyntaxRewriter
        {
            private List<SyntaxNodeOrToken> _elements;
            private int _index;

            public SyntaxNodeOrToken Reassemble(SyntaxNodeOrToken original, List<SyntaxNodeOrToken> rewrittenElements)
            {
                _elements = rewrittenElements;
                _index = 0;
                return ((CSharpSyntaxNode)original).Accept(this);
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                return _elements[_index++].AsNode();
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return _elements[_index++].AsToken();
            }
        }

        private class TriviaRewriter : CSharpSyntaxRewriter
        {
            private CSharpNonRecursiveSyntaxRewriter _parent;

            public TriviaRewriter(CSharpNonRecursiveSyntaxRewriter parent, bool visitIntoStructuredTrivia)
                : base(visitIntoStructuredTrivia)
            {
                _parent = parent;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                // allow recursion for structured trivia nodes (this is only happens once, maybe twice..)
                return _parent.Rewrite(node);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                var rewritten = base.VisitTrivia(trivia);
                return _parent.VisitTrivia(trivia, rewritten);
            }
        }
    }
}