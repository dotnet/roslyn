using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxNodeReplacer
    {
        internal static TRoot Replace<TRoot, TNode>(TRoot root, TNode oldNode, TNode newNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            if (oldNode == newNode)
            {
                return root;
            }

            return (TRoot)new SingleNodeReplacer(oldNode, newNode).Visit(root);
        }

        internal static TRoot Replace<TRoot, TNode>(TRoot root, IEnumerable<TNode> oldNodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            var oldNodesArray = oldNodes.ToArray();
            if (oldNodesArray.Length == 0)
            {
                return root;
            }

            return (TRoot)new MultipleNodeReplacer(oldNodesArray, (node, rewritten) => computeReplacementNode((TNode)node, (TNode)rewritten)).Visit(root);
        }

        private class ReplacerBase : SyntaxRewriter
        {
            public ReplacerBase(bool visitIntoStructuredTrivia)
                : base(visitIntoStructuredTrivia)
            {
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                // only bother visiting the token (and its trivia) if we are replacing a node in structured trivia
                if (this.VisitIntoStructuredTrivia)
                {
                    return base.VisitToken(token);
                }
                else
                {
                    return token;
                }
            }
        }

        private class SingleNodeReplacer : ReplacerBase
        {
            private readonly SyntaxNode oldNode;
            private readonly SyntaxNode newNode;
            private readonly TextSpan oldNodeFullSpan;

            public SingleNodeReplacer(SyntaxNode oldNode, SyntaxNode newNode) :
                base(oldNode.IsPartOfStructuredTrivia())
            {
                this.oldNode = oldNode;
                this.newNode = newNode;
                this.oldNodeFullSpan = oldNode.FullSpan;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    if (node == this.oldNode)
                    {
                        return this.newNode;
                    }

                    if (node.FullSpan.IntersectsWith(this.oldNodeFullSpan))
                    {
                        return base.Visit(node);
                    }
                }

                return node;
            }
        }

        private class MultipleNodeReplacer : ReplacerBase
        {
            private readonly SyntaxNode[] nodes;
            private readonly HashSet<SyntaxNode> nodeSet;
            private readonly TextSpan totalSpan;
            private readonly Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode;

            public MultipleNodeReplacer(SyntaxNode[] nodes, Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode) :
                base(nodes.Any(n => n.IsPartOfStructuredTrivia()))
            {
                this.nodes = nodes;
                this.nodeSet = new HashSet<SyntaxNode>(this.nodes);
                this.totalSpan = ComputeTotalSpan(this.nodes);
                this.computeReplacementNode = computeReplacementNode;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                var result = node;

                if (node != null)
                {
                    if (this.ShouldVisit(node))
                    {
                        result = base.Visit(node);
                    }

                    if (this.nodeSet.Contains(node))
                    {
                        result = this.computeReplacementNode(node, result);
                    }
                }

                return result;
            }

            private static TextSpan ComputeTotalSpan(SyntaxNode[] nodes)
            {
                var span0 = nodes[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;

                for (int i = 1; i < nodes.Length; i++)
                {
                    var span = nodes[i].FullSpan;
                    start = Math.Min(start, span.Start);
                    end = Math.Max(end, span.End);
                }

                return new TextSpan(start, end - start);
            }

            private bool ShouldVisit(SyntaxNode node)
            {
                var span = node.Span;

                // first do quick check against total span
                if (!span.IntersectsWith(this.totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var n in this.nodes)
                {
                    if (span.IntersectsWith(n.FullSpan))
                    {
                        // node's full span intersects with at least one node to be replaced
                        // so we need to visit node's children to find it.
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
