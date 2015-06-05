// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
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

        public new void Visit(SyntaxNode node)
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
                        base.Visit(en.Node);
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

        private void WalkToken(SyntaxToken token)
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

        private void WalkTriviaList(SyntaxTriviaList list)
        {
            foreach (var tr in list)
            {
                this.WalkTrivia(tr);
            }
        }

        private void WalkTrivia(SyntaxTrivia trivia)
        {
            if (this.Depth >= SyntaxWalkerDepth.StructuredTrivia && trivia.HasStructure)
            {
                this.Visit((CSharpSyntaxNode)trivia.GetStructure());
            }

            this.VisitTrivia(trivia);
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

        public virtual void VisitToken(SyntaxToken token)
        {
        }

        public virtual void VisitTrivia(SyntaxTrivia trivia)
        {
        }
    }
}