// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal sealed partial class CSharpMiscellaneousReducer
{
    private sealed class Rewriter : AbstractReductionRewriter
    {
        public Rewriter(ObjectPool<IReductionRewriter> pool)
            : base(pool)
        {
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
            => SimplifyNode(
                node,
                newNode: base.VisitParameter(node),
                simplifier: s_simplifyParameter);

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            => SimplifyNode(
                node,
                newNode: base.VisitParenthesizedLambdaExpression(node),
                simplifier: s_simplifyParenthesizedLambdaExpression);

        public override SyntaxNode? VisitBlock(BlockSyntax node)
            => SimplifyNode(
                node,
                newNode: base.VisitBlock(node),
                simplifier: s_simplifyBlock);
    }
}
