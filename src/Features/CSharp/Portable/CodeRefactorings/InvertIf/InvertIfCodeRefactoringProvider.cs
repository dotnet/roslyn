// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private static readonly Dictionary<SyntaxKind, (SyntaxKind, SyntaxKind)> s_binaryMap =
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

            // Tweak 
            var span = TextSpan.FromBounds(ifStatement.GetFirstToken().Span.Start, ifStatement.CloseParenToken.Span.End);
            if (!span.IntersectsWith(textSpan))
            {
                return null;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return null;
            }
            return ifStatement;
        }

        protected override Task<SyntaxNode> InvertIfStatementAsync(Document document, SemanticModel model, SyntaxNode ifStatement, CancellationToken cancellationToken)
        {
            var ifNode = (IfStatementSyntax)ifStatement;

            // In the case that the else clause is actually an else if clause, place the if
            // statement to be moved in a new block in order to make sure that the else
            // statement matches the right if statement after the edit.
            var newIfNodeStatement = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement
                ? SyntaxFactory.Block(ifNode.Else.Statement)
                : ifNode.Else.Statement;

            ifNode = ifNode.WithCondition(Negate(ifNode.Condition, model, cancellationToken))
                .WithStatement(newIfNodeStatement)
                .WithElse(ifNode.Else.WithStatement(ifNode.Statement))
                .WithAdditionalAnnotations(Formatter.Annotation);

            return Task.FromResult<SyntaxNode>(ifNode);
        }

        private bool TryNegateBinaryComparisonExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax result)
        {
            if (s_binaryMap.TryGetValue(expression.Kind(), out var negatedExpressionInfo))
            {
                var binaryExpression = (BinaryExpressionSyntax)expression;
                var (negatedExpressionType, negatedOperatorType) = negatedExpressionInfo;

                // Certain expressions can never be negative, such as length or unsigned numeric types.
                // If these expressions are compared with zero using <, >, or =, we construct the negated
                // binary expression to reflect that it will never be negative.
                // For example, the expression Array.Length > 0, becomes Array.Length == 0 when negated.
                var operation = semanticModel.GetOperation(binaryExpression);
                if (IsSpecialCaseBinaryExpression(operation as IBinaryOperation, cancellationToken))
                {
                    negatedOperatorType = binaryExpression.OperatorToken.Kind() == SyntaxKind.EqualsEqualsToken
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanToken : SyntaxKind.LessThanToken
                        : SyntaxKind.EqualsEqualsToken;
                    negatedExpressionType = binaryExpression.Kind() == SyntaxKind.EqualsExpression
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanExpression : SyntaxKind.LessThanExpression
                        : SyntaxKind.EqualsExpression;
                }

                result = SyntaxFactory.BinaryExpression(
                    negatedExpressionType,
                    binaryExpression.Left,
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        negatedOperatorType,
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
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)expression;
                    return parenthesizedExpression
                        .WithExpression(Negate(parenthesizedExpression.Expression, semanticModel, cancellationToken))
                        .WithAdditionalAnnotations(Simplifier.Annotation);
                }

                case SyntaxKind.LogicalNotExpression:
                {
                    var logicalNotExpression = (PrefixUnaryExpressionSyntax)expression;

                    var notToken = logicalNotExpression.OperatorToken;
                    var nextToken = logicalNotExpression.Operand.GetFirstToken(
                        includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

                    var existingTrivia = SyntaxFactory.TriviaList(
                        notToken.LeadingTrivia.Concat(
                            notToken.TrailingTrivia.Concat(
                                nextToken.LeadingTrivia)));

                    var updatedNextToken = nextToken.WithLeadingTrivia(existingTrivia);

                    return logicalNotExpression.Operand.ReplaceToken(
                        nextToken,
                        updatedNextToken);
                }

                case SyntaxKind.LogicalOrExpression:
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

                case SyntaxKind.LogicalAndExpression:
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

                case SyntaxKind.TrueLiteralExpression:
                {
                    var literalExpression = (LiteralExpressionSyntax)expression;
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.FalseLiteralExpression,
                        SyntaxFactory.Token(
                            literalExpression.Token.LeadingTrivia,
                            SyntaxKind.FalseKeyword,
                            literalExpression.Token.TrailingTrivia));
                }

                case SyntaxKind.FalseLiteralExpression:
                {
                    var literalExpression = (LiteralExpressionSyntax)expression;
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.TrueLiteralExpression,
                        SyntaxFactory.Token(
                            literalExpression.Token.LeadingTrivia,
                            SyntaxKind.TrueKeyword,
                            literalExpression.Token.TrailingTrivia));
                }
            }

            // Anything else we can just negate by adding a ! in front of the parenthesized expression.
            // Unnecessary parentheses will get removed by the simplification service.
            return SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                expression.Parenthesize());
        }
    }
}
