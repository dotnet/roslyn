// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Precedence;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider :
        AbstractInlineMethodRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax>
    {
        private static readonly ImmutableArray<SyntaxKind> s_leftAssociativeSyntaxKinds =
            ImmutableArray.Create(
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
                SyntaxKind.GreaterThanOrEqualExpression);

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKindsNeedsToCheckThePrecedence =
            ImmutableArray.Create(
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
                SyntaxKind.ConditionalAccessExpression,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.InvocationExpression,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ElementAccessExpression,
                SyntaxKind.SwitchExpression,
                SyntaxKind.ConditionalExpression,
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
                SyntaxKind.LeftShiftAssignmentExpression
            );

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKindsConsideredAsStatementInvokesCallee =
            ImmutableArray.Create(
                SyntaxKind.DoStatement,
                SyntaxKind.ExpressionStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.IfStatement,
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.SwitchStatement,
                SyntaxKind.ThrowStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.TryStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.YieldReturnStatement);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpExpressionPrecedenceService.Instance)
        {
        }

        private static bool ShouldStatementBeInlined(StatementSyntax statementSyntax)
            => statementSyntax is ReturnStatementSyntax || statementSyntax is ExpressionStatementSyntax;

        protected override bool IsSingleStatementOrExpressionMethod(SyntaxNode calleeMethodDeclarationSyntaxNode)
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
                    var arrowExpressionNodes = declarationSyntax.ExpressionBody;
                    return arrowExpressionNodes != null;
                }
            }

            return false;
        }

        protected override IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel,ArgumentSyntax argumentSyntaxNode, CancellationToken cancellationToken)
            => argumentSyntaxNode.DetermineParameter(semanticModel, allowParams: true, cancellationToken);

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

                return inlineSyntaxNode;
            }
            else
            {
                // 2. If it is using Arrow Expression
                var arrowExpressionNode = declarationSyntax.ExpressionBody;
                if (arrowExpressionNode != null)
                {
                    inlineSyntaxNode = arrowExpressionNode.Expression;
                }

                return inlineSyntaxNode;
            }

            // A check has been done before to make sure there is one arrow expression or block statement.
            throw ExceptionUtilities.Unreachable;
        }

        private static SyntaxNode? GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                _ => null
            };

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

            // In other cases, always put a 'safe' parenthesis around
            return true;
        }

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol)
            => symbol.GenerateTypeSyntax(allowVar: false);

        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

        protected override bool IsStatementConsideredAsInvokingStatement(SyntaxNode node)
            => s_syntaxKindsConsideredAsStatementInvokesCallee.Any(node.IsKind);
    }
}
