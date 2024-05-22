// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpCastReducer
{
    private class Rewriter : AbstractReductionRewriter
    {
        public Rewriter(ObjectPool<IReductionRewriter> pool)
            : base(pool)
        {
        }

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitCastExpression(node),
                simplifier: s_simplifyCast);
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var result = base.VisitBinaryExpression(node);
            var reducedNode = result as BinaryExpressionSyntax;
            if (reducedNode != node && reducedNode != null)
            {
                if ((node.Left.IsKind(SyntaxKind.CastExpression) && !reducedNode.Left.IsKind(SyntaxKind.CastExpression)) ||
                    (node.Right.IsKind(SyntaxKind.CastExpression) && !reducedNode.Right.IsKind(SyntaxKind.CastExpression)))
                {
                    // Cast simplification inside a binary expression, check if we need to parenthesize the binary expression to avoid breaking parent syntax.
                    // For example, cast removal in below case leads to syntax errors in error free code, unless parenting binary expression is parenthesized:
                    //   Original:                  Goo(x < (int)i, x > y)
                    //   Incorrect cast removal:    Goo(x < i, x > y)
                    //   Correct cast removal:      Goo((x < i), x > y)

                    // We'll do the following to detect such cases:
                    // 1) Get the topmostExpressionAncestor of node.
                    // 2) Get the reducedAncestor after replacing node with reducedNode within it.
                    // 3) Reparse the reducedAncestor to get reparsedAncestor.
                    // 4) Check for syntax equivalence of reducedAncestor and reparsedAncestor. If not syntactically equivalent,
                    //    then cast removal breaks the syntax and needs explicit parentheses around the binary expression.

                    var topmostExpressionAncestor = node
                        .AncestorsAndSelf()
                        .OfType<ExpressionSyntax>()
                        .LastOrDefault();

                    if (topmostExpressionAncestor != null && topmostExpressionAncestor != node)
                    {
                        var reducedAncestor = topmostExpressionAncestor.ReplaceNode(node, reducedNode);
                        var reparsedAncestor = SyntaxFactory.ParseExpression(reducedAncestor.ToFullString());
                        if (reparsedAncestor != null && !reparsedAncestor.IsEquivalentTo(reducedAncestor))
                        {
                            return SyntaxFactory.ParenthesizedExpression(reducedNode)
                                .WithAdditionalAnnotations(Simplifier.Annotation);
                        }
                    }
                }
            }

            return result;
        }
    }
}
