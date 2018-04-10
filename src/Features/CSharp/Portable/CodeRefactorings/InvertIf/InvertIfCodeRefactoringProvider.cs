// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal partial class InvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider
    {
        private static readonly Dictionary<SyntaxKind, (SyntaxKind negatedBinaryExpression, SyntaxKind negatedToken)> s_negatedBinaryMap =
            new Dictionary<SyntaxKind, (SyntaxKind, SyntaxKind)>(SyntaxFacts.EqualityComparer)
                {
                    { SyntaxKind.EqualsExpression, (SyntaxKind.NotEqualsExpression, SyntaxKind.ExclamationEqualsToken) },
                    { SyntaxKind.NotEqualsExpression, (SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken) },
                    { SyntaxKind.LessThanExpression, (SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken) },
                    { SyntaxKind.LessThanOrEqualExpression, (SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken) },
                    { SyntaxKind.GreaterThanExpression, (SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken) },
                    { SyntaxKind.GreaterThanOrEqualExpression, (SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken) },
                };

        protected override SyntaxNode GetIfStatement(TextSpan textSpan, SyntaxToken token, CancellationToken cancellationToken)
        {
            var ifStatement = token.GetAncestor<IfStatementSyntax>();
            if (ifStatement == null || ifStatement.Else == null)
            {
                return null;
            }

            var span = TextSpan.FromBounds(ifStatement.GetFirstToken().Span.Start, ifStatement.CloseParenToken.Span.End);
            if (!span.IntersectsWith(textSpan))
            {
                return null;
            }

            return ifStatement;
        }

        protected override SyntaxNode GetRootWithInvertIfStatement(
            Workspace workspace, 
            SemanticModel model, 
            SyntaxNode ifStatement, 
            CancellationToken cancellationToken)
        {
            var ifNode = (IfStatementSyntax)ifStatement;

            // For single line statment, we swap the TrailingTrivia to preserve the single line
            StatementSyntax newIfNodeStatement = null;
            ElseClauseSyntax newElseStatement = null;

            var isMultiLine = ifNode.Statement.GetTrailingTrivia().Any(trivia => trivia.Kind() == SyntaxKind.EndOfLineTrivia);
            if (isMultiLine)
            {
                newIfNodeStatement = ifNode.Else.Statement.Kind() != SyntaxKind.Block
                    ? SyntaxFactory.Block(ifNode.Else.Statement)
                    : ifNode.Else.Statement;
                newElseStatement = ifNode.Else.WithStatement(ifNode.Statement);
            }
            else
            {
                var elseTrailingTrivia = ifNode.Else.GetTrailingTrivia();
                var ifTrailingTrivia = ifNode.Statement.GetTrailingTrivia();
                newIfNodeStatement = ifNode.Else.Statement.WithTrailingTrivia(ifTrailingTrivia);
                newElseStatement = ifNode.Else.WithStatement(ifNode.Statement).WithTrailingTrivia(elseTrailingTrivia);
            }

            var newIfStatment = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement && newIfNodeStatement.Kind() != SyntaxKind.Block
                        ? SyntaxFactory.Block(newIfNodeStatement)
                        : newIfNodeStatement;

            ifNode = ifNode.WithCondition(Negate(ifNode.Condition, model, cancellationToken))
                .WithStatement(newIfStatment)
                .WithElse(newElseStatement);

            if (isMultiLine)
            {
                ifNode = ifNode.WithAdditionalAnnotations(Formatter.Annotation);
            }

            // get new root
            return model.SyntaxTree.GetRoot().ReplaceNode(ifStatement, ifNode);
        }

        private bool TryNegateBinaryComparisonExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax result)
        {
            if (s_negatedBinaryMap.TryGetValue(expression.Kind(), out var negatedExpressionInfo))
            {
                var binaryExpression = (BinaryExpressionSyntax)expression;
                var (negatedBinaryExpression, negatedToken) = negatedExpressionInfo;

                // Certain expressions can never be negative, such as length or unsigned numeric types.
                // If these expressions are compared with zero using <, >, or =, we construct the negated
                // binary expression to reflect that it will never be negative.
                // For example, the expression Array.Length > 0, becomes Array.Length == 0 when negated.
                var operation = semanticModel.GetOperation(binaryExpression);

                if (IsSpecialCaseBinaryExpression(operation as IBinaryOperation, cancellationToken))
                {
                    negatedToken = binaryExpression.OperatorToken.Kind() == SyntaxKind.EqualsEqualsToken
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanToken : SyntaxKind.LessThanToken
                        : SyntaxKind.EqualsEqualsToken;
                    negatedBinaryExpression = binaryExpression.Kind() == SyntaxKind.EqualsExpression
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanExpression 
                            : SyntaxKind.LessThanExpression
                        : SyntaxKind.EqualsExpression;
                }

                result = SyntaxFactory.BinaryExpression(
                    negatedBinaryExpression,
                    binaryExpression.Left,
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        negatedToken,
                        binaryExpression.OperatorToken.TrailingTrivia),
                    binaryExpression.Right);

                return true;
            }

            result = null;
            return false;
        }

        private ExpressionSyntax Negate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryNegateBinaryComparisonExpression(expression, semanticModel, cancellationToken, out var result))
            {
                return result;
            }

            switch (expression.Kind())
            {
                case SyntaxKind.ParenthesizedExpression:
                    {
                        return GetNegatedParenthesizedExpression(expression, semanticModel, cancellationToken);
                    }

                case SyntaxKind.LogicalNotExpression:
                    {
                        return GetNegatedLogicalNotExpression(expression);
                    }

                case SyntaxKind.LogicalOrExpression:
                    {
                        return GetNegatedLogicalOrExpression(expression, semanticModel, out result, cancellationToken);
                    }

                case SyntaxKind.LogicalAndExpression:
                    {
                        return GetNegatedLogicalAndExpression(expression, semanticModel, out result, cancellationToken);
                    }

                case SyntaxKind.TrueLiteralExpression:
                    {
                        return GetNegatedTrueLiteralExpression(expression);
                    }

                case SyntaxKind.FalseLiteralExpression:
                    {
                        return GetNegatedFalseLiteralExpression(expression);
                    }
            }

            // Anything else we can just negate by adding a ! in front of the parenthesized expression.
            // Unnecessary parentheses will get removed by the simplification service.
            return SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                expression.Parenthesize());
        }

        private static ExpressionSyntax GetNegatedFalseLiteralExpression(ExpressionSyntax expression)
        {
            var literalExpression = (LiteralExpressionSyntax)expression;
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.TrueLiteralExpression,
                SyntaxFactory.Token(
                    literalExpression.Token.LeadingTrivia,
                    SyntaxKind.TrueKeyword,
                    literalExpression.Token.TrailingTrivia));
        }

        private static ExpressionSyntax GetNegatedTrueLiteralExpression(ExpressionSyntax expression)
        {
            var literalExpression = (LiteralExpressionSyntax)expression;
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.FalseLiteralExpression,
                SyntaxFactory.Token(
                    literalExpression.Token.LeadingTrivia,
                    SyntaxKind.FalseKeyword,
                    literalExpression.Token.TrailingTrivia));
        }

        private ExpressionSyntax GetNegatedLogicalAndExpression(ExpressionSyntax expression, SemanticModel semanticModel, out ExpressionSyntax result, CancellationToken cancellationToken)
        {
            var binaryExpression = (BinaryExpressionSyntax)expression;
            result = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                Negate(binaryExpression.Left, semanticModel, cancellationToken),
                SyntaxFactory.Token(
                    binaryExpression.OperatorToken.LeadingTrivia,
                    SyntaxKind.BarBarToken,
                    binaryExpression.OperatorToken.TrailingTrivia),
                Negate(binaryExpression.Right, semanticModel, cancellationToken));

            return result
                .Parenthesize()
                .WithLeadingTrivia(binaryExpression.GetLeadingTrivia())
                .WithTrailingTrivia(binaryExpression.GetTrailingTrivia());
        }

        private ExpressionSyntax GetNegatedLogicalOrExpression(ExpressionSyntax expression, SemanticModel semanticModel, out ExpressionSyntax result, CancellationToken cancellationToken)
        {
            var binaryExpression = (BinaryExpressionSyntax)expression;
            result = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                Negate(binaryExpression.Left, semanticModel, cancellationToken),
                SyntaxFactory.Token(
                    binaryExpression.OperatorToken.LeadingTrivia,
                    SyntaxKind.AmpersandAmpersandToken,
                    binaryExpression.OperatorToken.TrailingTrivia),
                Negate(binaryExpression.Right, semanticModel, cancellationToken));

            return result
                .Parenthesize()
                .WithLeadingTrivia(binaryExpression.GetLeadingTrivia())
                .WithTrailingTrivia(binaryExpression.GetTrailingTrivia());
        }

        private static ExpressionSyntax GetNegatedLogicalNotExpression(ExpressionSyntax expression)
        {
            var logicalNotExpression = (PrefixUnaryExpressionSyntax)expression;

            var notToken = logicalNotExpression.OperatorToken;
            var nextToken = logicalNotExpression.Operand.GetFirstToken(
                includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

            // any trivia attached to the ! operator should be moved to the identifier's leading trivia
            var existingTrivia = SyntaxFactory.TriviaList(
                notToken.LeadingTrivia.Concat(
                    notToken.TrailingTrivia.Concat(
                        nextToken.LeadingTrivia)));

            var updatedNextToken = nextToken.WithLeadingTrivia(existingTrivia);

            // Since we're negating a !expr, just remove the existing !
            return logicalNotExpression.Operand.ReplaceToken(
                nextToken,
                updatedNextToken);
        }

        private ExpressionSyntax GetNegatedParenthesizedExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var parenthesizedExpression = (ParenthesizedExpressionSyntax)expression;
            return parenthesizedExpression
                .WithExpression(Negate(parenthesizedExpression.Expression, semanticModel, cancellationToken))
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        internal override string GetInvertIfText()
        {
            return CSharpFeaturesResources.Invert_if;
        }
    }
}
