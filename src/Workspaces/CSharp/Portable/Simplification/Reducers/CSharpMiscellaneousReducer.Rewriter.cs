// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpMiscellaneousReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
            }

            public override SyntaxNode VisitParameter(ParameterSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitParameter(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyParameter);
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitParenthesizedLambdaExpression(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyParenthesizedLambdaExpression);
            }

            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitBlock(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyBlock);
            }
        }
    }
}
