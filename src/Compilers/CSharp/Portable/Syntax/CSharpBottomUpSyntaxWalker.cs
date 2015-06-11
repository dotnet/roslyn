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
    /// A tree walker that visits children before visiting parents.
    /// </summary>
    public abstract partial class CSharpBottomUpSyntaxWalker : CSharpSyntaxVisitor
    {
        private static ObjectPool<List<ChildSyntaxList.Enumerator>> s_listPool
            = new ObjectPool<List<ChildSyntaxList.Enumerator>>(() => new List<ChildSyntaxList.Enumerator>(20), 30);

        private List<ChildSyntaxList.Enumerator> stack;

        protected SyntaxWalkerDepth Depth { get; }

        protected CSharpBottomUpSyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
        {
            this.Depth = depth;
        }

        [Obsolete("Use WalkNode to initiate walking.")]
        public new void Visit(SyntaxNode node)
        {
        }

        /// <summary>
        /// Walks the subtree by visiting all children (including all descendants) and then this node.
        /// </summary>
        public void WalkNode(SyntaxNode node)
        {
            bool allocated = false;
            if (stack != null)
            {
                stack = s_listPool.Allocate();
                allocated = true;
            }

            try
            {
                int start = stack.Count;
                if (this.CanVisit(node))
                {
                    stack.Add(node.ChildNodesAndTokens().GetEnumerator());
                }

                while (stack.Count > start)
                {
                    var en = stack[stack.Count - 1];
                    if (en.MoveNext())
                    {
                        stack[stack.Count - 1] = en;

                        var item = en.Current;
                        if (item.IsNode)
                        {
                            var nd = item.AsNode();
                            if (this.CanVisit(nd))
                            {
                                stack.Add(nd.ChildNodesAndTokens().GetEnumerator());
                            }
                        }
                        else if (item.IsToken && this.Depth >= SyntaxWalkerDepth.Token)
                        {
                            var tk = item.AsToken();
                            if (this.CanVisit(tk))
                            {
                                this.WalkToken(tk);
                            }
                        }
                    }
                    else
                    {
                        stack.RemoveAt(stack.Count - 1);

                        // after all children visit node here..
                        this.VisitNode(en.Node);
                    }
                }
            }
            finally
            {
                if (allocated)
                {
                    s_listPool.Free(stack);
                }
            }
        }

        /// <summary>
        /// Walks the token by visiting the token's leading trivia, then the token, and then the trailing trivia.
        /// </summary>
        public void WalkToken(SyntaxToken token)
        {
            if (this.Depth >= SyntaxWalkerDepth.Trivia)
            {
                this.WalkTriviaList(token.LeadingTrivia);
                this.VisitToken(token);
                this.WalkTriviaList(token.TrailingTrivia);
            }
            else
            {
                this.VisitToken(token);
            }
        }

        /// <summary>
        /// Walks the list by visiting each trivia in source order and then the list.
        /// </summary>
        private void WalkTriviaList(SyntaxTriviaList list)
        {
            foreach (var tr in list)
            {
                this.WalkTrivia(tr);
            }
        }

        /// <summary>
        /// Walks the trivia by first visiting the trivia's structure (if it has structure) and then the trivia itself.
        /// </summary>
        public void WalkTrivia(SyntaxTrivia trivia)
        {
            if (this.Depth >= SyntaxWalkerDepth.StructuredTrivia && trivia.HasStructure)
            {
                this.WalkNode((CSharpSyntaxNode)trivia.GetStructure());
            }

            this.VisitTrivia(trivia);
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
        public virtual void VisitNode(SyntaxNode node)
        {
            base.Visit(node);
        }

        /// <summary>
        /// Called after leading trivia has been visited, but before trailing trivia has been visited.
        /// </summary>
        public virtual void VisitToken(SyntaxToken token)
        {
        }

        /// <summary>
        /// Called after any structure has been visited.
        /// </summary>
        public virtual void VisitTrivia(SyntaxTrivia trivia)
        {
        }
    }
}