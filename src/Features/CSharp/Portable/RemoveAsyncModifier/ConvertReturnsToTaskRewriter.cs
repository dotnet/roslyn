// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier
{
    internal partial class CSharpRemoveAsyncModifierCodeFixProvider
    {
        private class ConvertReturnsToTaskRewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxGenerator _generator;
            private readonly INamedTypeSymbol _taskType;

            public ConvertReturnsToTaskRewriter(SyntaxGenerator generator, INamedTypeSymbol taskType)
            {
                _generator = generator;
                _taskType = taskType;
            }

            public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
            {
                if (node.Expression is null)
                {
                    return node.WithExpression((ExpressionSyntax)_generator.MemberAccessExpression(_generator.TypeExpression(_taskType), nameof(Task.CompletedTask)));
                }
                return node.WithExpression(WrapWithTaskFromResult(node.Expression));
            }

            public override SyntaxNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                return node.WithExpression(WrapWithTaskFromResult(node.Expression));
            }

            public ExpressionSyntax WrapWithTaskFromResult(ExpressionSyntax expression)
            {
                return (ExpressionSyntax)
                    _generator.InvocationExpression(
                        _generator.MemberAccessExpression(
                            _generator.TypeExpression(_taskType), nameof(Task.FromResult)),
                            expression);
            }
        }
    }
}
