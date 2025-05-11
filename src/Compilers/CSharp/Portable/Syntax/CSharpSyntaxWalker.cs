// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a <see cref="CSharpSyntaxVisitor"/> that descends an entire <see cref="CSharpSyntaxNode"/> graph
    /// visiting each CSharpSyntaxNode and its child SyntaxNodes and <see cref="SyntaxToken"/>s in depth-first order.
    /// </summary>
    public abstract class CSharpSyntaxWalker : CSharpSyntaxVisitor
    {
        protected SyntaxWalkerDepth Depth { get; }

        protected CSharpSyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
        {
            this.Depth = depth;
        }

        private int _recursionDepth;

        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                ((CSharpSyntaxNode)node).Accept(this);

                _recursionDepth--;
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            var childCnt = node.ChildNodesAndTokens().Count;
            int i = 0;
            var slotData = new ChildSyntaxList.SlotData(node);

            do
            {
                var child = ChildSyntaxList.ItemInternal((CSharpSyntaxNode)node, i, ref slotData);
                i++;

                var asNode = child.AsNode();
                if (asNode != null)
                {
                    if (this.Depth >= SyntaxWalkerDepth.Node)
                    {
                        this.Visit(asNode);
                    }
                }
                else
                {
                    if (this.Depth >= SyntaxWalkerDepth.Token)
                    {
                        this.VisitToken(child.AsToken());
                    }
                }
            } while (i < childCnt);
        }

        public virtual void VisitToken(SyntaxToken token)
        {
            if (this.Depth >= SyntaxWalkerDepth.Trivia)
            {
                this.VisitLeadingTrivia(token);
                this.VisitTrailingTrivia(token);
            }
        }

        public virtual void VisitLeadingTrivia(SyntaxToken token)
        {
            if (token.HasLeadingTrivia)
            {
                foreach (var tr in token.LeadingTrivia)
                {
                    this.VisitTrivia(tr);
                }
            }
        }

        public virtual void VisitTrailingTrivia(SyntaxToken token)
        {
            if (token.HasTrailingTrivia)
            {
                foreach (var tr in token.TrailingTrivia)
                {
                    this.VisitTrivia(tr);
                }
            }
        }

        public virtual void VisitTrivia(SyntaxTrivia trivia)
        {
            if (this.Depth >= SyntaxWalkerDepth.StructuredTrivia && trivia.HasStructure)
            {
                this.Visit((CSharpSyntaxNode)trivia.GetStructure()!);
            }
        }
    }
}
