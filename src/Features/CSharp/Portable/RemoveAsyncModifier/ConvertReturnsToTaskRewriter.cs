// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
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
                    // return; ==> return Task.CompletedTask;
                    return node.WithExpression((ExpressionSyntax)_generator.MemberAccessExpression(TypeExpressionForStaticMemberAccess(_generator, _taskType), nameof(Task.CompletedTask)));
                }
                // return <expr>; ==> return Task.FromResult(<expr>);
                return node.WithExpression(WrapWithTaskFromResult(node.Expression));
            }

            public override SyntaxNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                // For expression bodied methods and local functions just wrap the whole thing in Task.FromResult
                return node.WithExpression(WrapWithTaskFromResult(node.Expression));
            }

            public ExpressionSyntax WrapWithTaskFromResult(ExpressionSyntax expression)
            {
                // <expr>; ==> Task.FromResult(<expr>);
                return (ExpressionSyntax)
                    _generator.InvocationExpression(
                        _generator.MemberAccessExpression(
                            TypeExpressionForStaticMemberAccess(_generator, _taskType), nameof(Task.FromResult)),
                            expression);
            }

            // Workaround for https://github.com/dotnet/roslyn/issues/43950
            // Copied from https://github.com/dotnet/roslyn-analyzers/blob/f24a5b42c85be6ee572f3a93bef223767fbefd75/src/Utilities/Workspaces/SyntaxGeneratorExtensions.cs#L68-L74
            private static SyntaxNode TypeExpressionForStaticMemberAccess(SyntaxGenerator generator, INamedTypeSymbol typeSymbol)
            {
                var qualifiedNameSyntaxKind = generator.QualifiedName(generator.IdentifierName("ignored"), generator.IdentifierName("ignored")).RawKind;
                var memberAccessExpressionSyntaxKind = generator.MemberAccessExpression(generator.IdentifierName("ignored"), "ignored").RawKind;

                var typeExpression = generator.TypeExpression(typeSymbol);
                return QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, typeExpression, generator);

                // Local function
                static SyntaxNode QualifiedNameToMemberAccess(int qualifiedNameSyntaxKind, int memberAccessExpressionSyntaxKind, SyntaxNode expression, SyntaxGenerator generator)
                {
                    if (expression.RawKind == qualifiedNameSyntaxKind)
                    {
                        var left = QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, expression.ChildNodes().First(), generator);
                        var right = expression.ChildNodes().Last();
                        return generator.MemberAccessExpression(left, right);
                    }

                    return expression;
                }
            }
        }
    }
}
