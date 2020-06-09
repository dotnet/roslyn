// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpDefaultExpressionReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
                _simplifyDefaultExpression = SimplifyDefaultExpression;
            }

            private readonly Func<DefaultExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> _simplifyDefaultExpression;

            private SyntaxNode SimplifyDefaultExpression(
                DefaultExpressionSyntax node,
                SemanticModel semanticModel,
                OptionSet optionSet,
                CancellationToken cancellationToken)
            {
                var preferSimpleDefaultExpression = optionSet.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression).Value;

                if (node.CanReplaceWithDefaultLiteral(ParseOptions, preferSimpleDefaultExpression, semanticModel, cancellationToken))
                {
                    return SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                                        .WithTriviaFrom(node);
                }

                return node;
            }

            public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitDefaultExpression(node),
                    parentNode: node.Parent,
                    simplifier: _simplifyDefaultExpression);
            }
        }
    }
}
