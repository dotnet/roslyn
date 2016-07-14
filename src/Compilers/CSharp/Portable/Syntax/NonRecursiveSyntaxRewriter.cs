// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a non-recursive visitor which descends an entire <see cref="CSharpSyntaxNode"/> graph and
    /// may replace or remove visited SyntaxNodes in depth-first order.
    /// </summary>
    public partial class NonRecursiveSyntaxRewriter : CSharpSyntaxVisitor<NonRecursiveSyntaxRewriter.Chunk>
    {
        public class Chunk
        {
            private int currentIndex;

            internal Chunk(Func<SyntaxNodeOrToken> transform, SyntaxNodeOrToken[] children)
            {
                this.Transform = transform;
                this.ChildNodes = children;
                this.currentIndex = children.Length;
            }

            public Func<SyntaxNodeOrToken> Transform { get; set; }

            public SyntaxNodeOrToken[] ChildNodes { get; private set; }

            public bool MoveNextChildNode()
            {
                return this.currentIndex-- > 0;
            }

            public SyntaxNodeOrToken GetCurrentChild()
            {
                return ChildNodes[this.currentIndex];
            }
        }

        private Stack<SyntaxNodeOrToken> rewrittenStack = new Stack<SyntaxNodeOrToken>();
        protected internal Stack<SyntaxNodeOrToken> RewrittenStack
        {
            get { return this.rewrittenStack; }
        }

        public new SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            return this.Visit((SyntaxNodeOrToken)node);
        }

        public SyntaxToken Visit(SyntaxToken token)
        {
            return (SyntaxToken)this.VisitToken(token).Transform();
        }

        private SyntaxNode Visit(SyntaxNodeOrToken nodeOrToken)
        {
            Chunk chunk = this.VisitNode((CSharpSyntaxNode)nodeOrToken);
            var nodesToRewriteStack = new Stack<Chunk>();
            nodesToRewriteStack.Push(chunk);
            Queue<Chunk> rewriterQueue = new Queue<Chunk>();

            while (nodesToRewriteStack.Count != 0)
            {
                chunk = nodesToRewriteStack.Peek();

                if (!chunk.MoveNextChildNode())
                {
                    nodesToRewriteStack.Pop();
                    rewriterQueue.Enqueue(chunk);
                    continue;
                }

                SyntaxNodeOrToken subNodeOrToken = chunk.GetCurrentChild();
                if (subNodeOrToken.IsToken)
                {
                    nodesToRewriteStack.Push(this.VisitToken(subNodeOrToken.AsToken()));
                }
                else
                {
                    var subNode = (CSharpSyntaxNode)subNodeOrToken.AsNode();
                    if (subNode == null)
                    {
                        nodesToRewriteStack.Push(this.CreateChunk(null, () => null));
                        continue;
                    }

                    nodesToRewriteStack.Push(this.VisitNode(subNode));
                }
            }

            while (rewriterQueue.Count != 0)
            {
                this.RewrittenStack.Push(rewriterQueue.Dequeue().Transform());
            }

            if (this.RewrittenStack.Count != 1)
            {
                throw new InvalidOperationException();
            }

            return this.RewrittenStack.Pop().AsNode();
        }

        protected virtual Chunk CreateChunk(SyntaxNodeOrToken nodeOrToken, Func<SyntaxNodeOrToken> transform, params SyntaxNodeOrToken[] children)
        {
            return new Chunk(transform, children);
        }

        protected Chunk CreateChunk(SyntaxNodeOrToken nodeOrToken, Func<SyntaxNodeOrToken> transform, IEnumerable<SyntaxNodeOrToken> children)
        {
            return this.CreateChunk(nodeOrToken, transform, children.ToArray());
        }

        protected virtual Chunk VisitNode(CSharpSyntaxNode node)
        {
            return node.Accept(this);
        }

        public Chunk VisitToken(SyntaxToken token)
        {
            return this.CreateChunk(token, () => this.TransformToken(token));
        }

        protected virtual SyntaxToken TransformToken(SyntaxToken token)
        {
            var node = token.Node;
            if (node == null || !this.VisitIntoStructuredTrivia)
            {
                return token;
            }

            var leadingTrivia = node.GetLeadingTriviaCore();
            var trailingTrivia = node.GetTrailingTriviaCore();

            if (leadingTrivia != null)
            {
                // PERF: Expand token.LeadingTrivia when node is not null.
                var leading = this.VisitList(new SyntaxTriviaList(token, leadingTrivia));

                if (trailingTrivia != null)
                {
                    // Both leading and trailing trivia

                    // PERF: Expand token.TrailingTrivia when node is not null and leadingTrivia is not null.
                    // Also avoid node.Width because it makes a virtual call to GetText. Instead use node.FullWidth - trailingTrivia.FullWidth.
                    var index = leadingTrivia.IsList ? leadingTrivia.SlotCount : 1;
                    var trailing = this.VisitList(new SyntaxTriviaList(token, trailingTrivia, token.Position + node.FullWidth - trailingTrivia.FullWidth, index));

                    if (leading.Node != leadingTrivia)
                    {
                        token = token.WithLeadingTrivia(leading);
                    }

                    return trailing.Node != trailingTrivia ? token.WithTrailingTrivia(trailing) : token;
                }
                else
                {
                    // Leading trivia only
                    return leading.Node != leadingTrivia ? token.WithLeadingTrivia(leading) : token;
                }
            }
            else if (trailingTrivia != null)
            {
                // Trailing trivia only
                // PERF: Expand token.TrailingTrivia when node is not null and leading is null.
                // Also avoid node.Width because it makes a virtual call to GetText. Instead use node.FullWidth - trailingTrivia.FullWidth.
                var trailing = this.VisitList(new SyntaxTriviaList(token, trailingTrivia, token.Position + node.FullWidth - trailingTrivia.FullWidth, index: 0));
                return trailing.Node != trailingTrivia ? token.WithTrailingTrivia(trailing) : token;
            }

            return token;
        }

        public virtual SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (this.VisitIntoStructuredTrivia && trivia.HasStructure)
            {
                var structure = (CSharpSyntaxNode)trivia.GetStructure();
                var newStructure = (StructuredTriviaSyntax)this.Visit((SyntaxNode)structure);

                if (newStructure != structure)
                {
                    if (newStructure != null)
                    {
                        return SyntaxFactory.Trivia(newStructure);
                    }
                    else
                    {
                        return default(SyntaxTrivia);
                    }
                }
            }

            return trivia;
        }

        public virtual SyntaxTriviaList VisitList(SyntaxTriviaList list)
        {
            var count = list.Count;
            if (count != 0)
            {
                SyntaxTriviaListBuilder alternate = null;
                var index = -1;

                foreach (var item in list)
                {
                    index++;
                    var visited = this.VisitListElement(item);

                    //skip the null check since SyntaxTrivia is a value type
                    if (visited != item && alternate == null)
                    {
                        alternate = new SyntaxTriviaListBuilder(count);
                        alternate.Add(list, 0, index);
                    }

                    if (alternate != null && visited.Kind() != SyntaxKind.None)
                    {
                        alternate.Add(visited);
                    }
                }

                if (alternate != null)
                {
                    return alternate.ToList();
                }
            }

            return list;
        }

        public virtual SyntaxTrivia VisitListElement(SyntaxTrivia element)
        {
            return this.VisitTrivia(element);
        }

        private Collection PopCollection<Collection, T>(Collection originalList, Func<SyntaxNodeOrToken, T> cast, Func<T, T, bool> notEquals, Func<IEnumerable<T>, Collection> createCollection)
            where Collection : IReadOnlyList<T>
        {
            return this.PopCollection(originalList, originalList, cast, notEquals, createCollection);
        }

        private Collection PopCollection<Collection, T>(Collection originalList, IEnumerable<T> originalItems, Func<SyntaxNodeOrToken, T> cast, Func<T, T, bool> notEquals, Func<IEnumerable<T>, Collection> createCollection)
        {
            List<T> newList = new List<T>();
            bool updated = false;
            foreach (T originalItem in originalItems)
            {
                T item = cast(this.RewrittenStack.Pop());
                newList.Add(item);
                if (notEquals(item, originalItem))
                {
                    updated = true;
                }
            }

            return updated ? createCollection(newList) : originalList;
        }

        protected virtual SyntaxList<T> PopList<T>(SyntaxList<T> originalList)
            where T : SyntaxNode
        {
            return this.PopCollection(originalList, snot => (T)snot.AsNode(), (t1, t2) => t1 != t2, items => SyntaxFactory.List(items));
        }

        protected virtual SeparatedSyntaxList<T> PopList<T>(SeparatedSyntaxList<T> originalList)
            where T : SyntaxNode
        {
            return this.PopCollection(originalList, originalList.GetWithSeparators(), snot => snot, (t1, t2) => t1 != t2, items => SyntaxFactory.SeparatedList<T>(items));
        }

        protected virtual SyntaxTokenList PopList(SyntaxTokenList originalList)
        {
            return this.PopCollection(originalList, snot => snot.AsToken(), (t1, t2) => t1 != t2, items => SyntaxFactory.TokenList(items));
        }

        public virtual bool VisitIntoStructuredTrivia { get; set; }
    }
}
