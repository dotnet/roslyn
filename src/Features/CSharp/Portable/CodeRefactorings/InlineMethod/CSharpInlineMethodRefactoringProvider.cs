// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Precedence;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider : AbstractInlineMethodRefactoringProvider
    {
        private static readonly ImmutableArray<SyntaxKind> s_leftAssociativeSyntaxKinds =
            ImmutableArray.CreateRange(new[]
            {
                SyntaxKind.AddExpression,
                SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideExpression,
                SyntaxKind.ModuloExpression,
                SyntaxKind.LeftShiftExpression,
                SyntaxKind.RightShiftExpression,
                SyntaxKind.LogicalOrExpression,
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.BitwiseOrExpression,
                SyntaxKind.BitwiseAndExpression,
                SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
            });

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKindsNeedsToCheckThePrecedence =
            ImmutableArray.CreateRange(new[]
            {
                SyntaxKind.AddExpression,
                SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideExpression,
                SyntaxKind.ModuloExpression,
                SyntaxKind.LeftShiftExpression,
                SyntaxKind.RightShiftExpression,
                SyntaxKind.LogicalOrExpression,
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.BitwiseOrExpression,
                SyntaxKind.BitwiseAndExpression,
                SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.IsExpression,
                SyntaxKind.UnaryMinusExpression,
                SyntaxKind.UnaryPlusExpression,
                SyntaxKind.LogicalNotExpression,
                SyntaxKind.BitwiseNotExpression,
                SyntaxKind.CastExpression,
                SyntaxKind.IsPatternExpression,
                SyntaxKind.AsExpression,
                SyntaxKind.CoalesceExpression,
                SyntaxKind.AwaitExpression,
                SyntaxKind.SimpleMemberAccessExpression,
                // Example: Func<int, int, int> Add() => (i, j) => i + j;
                // var x = Add()(1, 2);
                SyntaxKind.InvocationExpression,
                // Example: Callee()[10]
                SyntaxKind.ElementAccessExpression,
                // Example: switch Callee() { ... }
                SyntaxKind.SwitchExpression,
                SyntaxKind.ConditionalAccessExpression,
                SyntaxKind.SuppressNullableWarningExpression,
                SyntaxKind.RangeExpression,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
            });

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpExpressionPrecedenceService.Instance)
        {
        }

        protected override async Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context)
        {
            var syntaxNode = await context.TryGetRelevantNodeAsync<InvocationExpressionSyntax>().ConfigureAwait(false);
            return syntaxNode;
        }

        private static bool ShouldStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax is ReturnStatementSyntax || statementSyntax is ExpressionStatementSyntax;

        protected override bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            if (calleeMethodDeclarationSyntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                var blockSyntaxNode = declarationSyntax.Body;
                // 1. If it is an ordinary method with block
                if (blockSyntaxNode != null)
                {
                    var blockStatements = blockSyntaxNode.Statements;
                    return blockStatements.Count == 1 && ShouldStatementBeInlined(blockStatements[0]);
                }
                else
                {
                    // 2. If it is an Arrow Expression
                    var arrowExpressionNodes = declarationSyntax
                        .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                    return arrowExpressionNodes.Length == 1;
                }
            }

            return false;
        }

        protected override IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel, SyntaxNode argumentSyntaxNode, CancellationToken cancellationToken)
            => argumentSyntaxNode is ArgumentSyntax argumentSyntax
                ? argumentSyntax.DetermineParameter(semanticModel, allowParams: true, cancellationToken)
                : null;

        protected override bool IsExpressionSyntax(SyntaxNode syntaxNode)
            => syntaxNode is ExpressionSyntax;

        protected override SyntaxNode GenerateLocalDeclarationStatement(string identifierTokenName, ITypeSymbol type)
            => SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    type.GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(identifierTokenName))));

        protected override SyntaxNode GenerateIdentifierNameSyntaxNode(string name)
            => SyntaxFactory.IdentifierName(name);

        protected override SyntaxNode? GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode)
        {
            var declarationSyntax = (MethodDeclarationSyntax)calleeMethodDeclarationSyntaxNode;
            SyntaxNode? inlineSyntaxNode = null;
            var blockSyntaxNode = declarationSyntax.Body;
            // 1. If it is a ordinary method with block
            if (blockSyntaxNode != null)
            {
                var blockStatements = blockSyntaxNode.Statements;
                if (blockStatements.Count == 1)
                {
                    inlineSyntaxNode = GetExpressionFromStatementSyntaxNode(blockStatements[0]);
                }
            }
            else
            {
                // 2. If it is using Arrow Expression
                var arrowExpressionNodes = declarationSyntax
                    .DescendantNodes().Where(node => node.IsKind(SyntaxKind.ArrowExpressionClause)).ToImmutableArray();
                if (arrowExpressionNodes.Length == 1)
                {
                    inlineSyntaxNode = ((ArrowExpressionClauseSyntax)arrowExpressionNodes[0]).Expression;
                }
            }

            return inlineSyntaxNode;
        }

        private static SyntaxNode? GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                _ => null
            };

        protected override bool IsEmbeddedStatementOwner(SyntaxNode syntaxNode)
            => syntaxNode.IsEmbeddedStatementOwner();

        protected override bool ShouldCheckTheExpressionPrecedenceInCallee(SyntaxNode syntaxNode)
            => s_syntaxKindsNeedsToCheckThePrecedence.Any(syntaxNode.IsKind);

        protected override bool NeedWrapInParenthesisWhenPrecedenceAreEqual(SyntaxNode calleeInvocationSyntaxNode)
        {
            var parent = calleeInvocationSyntaxNode.Parent;
            if (parent != null)
            {
                // For left associative expression
                // If in the original invocation, it is the left child. Since it is a left associative expression,
                // then it is safe to replace it without parenthesis.
                // Example:
                // int Callee(int i, int j) => i + j;
                // void Caller()
                // {
                //   var a = F(1, 2) - 3 - 2 - 1;
                // }
                if (s_leftAssociativeSyntaxKinds.Any(parent.IsKind) && parent is BinaryExpressionSyntax leftAssociativeBinaryExpressionSyntax)
                {
                    return leftAssociativeBinaryExpressionSyntax.Left != calleeInvocationSyntaxNode;
                }

                // Same for right associative expression, if the original invocation is the right child,
                // it is safe to replace it.
                if (parent.IsKind(SyntaxKind.CoalesceExpression)
                    && parent is BinaryExpressionSyntax rightAssociativeBinaryExpressionSyntax)
                {
                    return rightAssociativeBinaryExpressionSyntax.Right != calleeInvocationSyntaxNode;
                }
                else if (parent is ConditionalExpressionSyntax conditionalExpressionSyntax)
                {
                    return conditionalExpressionSyntax.WhenFalse != calleeInvocationSyntaxNode;
                }
                else if (parent is AssignmentExpressionSyntax assignmentExpressionSyntax)
                {
                    return assignmentExpressionSyntax.Right != calleeInvocationSyntaxNode;
                }
            }

            // In case some cases are missing, always put a 'safe' parenthesis around
            return true;
        }

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol)
            => symbol.GenerateTypeSyntax(allowVar: false);

        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));
    }
}
