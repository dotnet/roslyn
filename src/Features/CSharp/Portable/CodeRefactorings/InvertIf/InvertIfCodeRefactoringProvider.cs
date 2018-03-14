// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal partial class InvertIfCodeRefactoringProvider : CodeRefactoringProvider
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

        private const string LongLength = "LongLength";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ifStatement = root.FindToken(textSpan.Start).GetAncestor<IfStatementSyntax>();
            if (ifStatement == null || ifStatement.Else == null)
            {
                return;
            }

            if (!ifStatement.IfKeyword.Span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Invert_if_statement,
                    c => InvertIfAsync(document, ifStatement, c)));
        }

        private async Task<Document> InvertIfAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var ifNode = ifStatement;

            // In the case that the else clause is actually an else if clause, place the if
            // statement to be moved in a new block in order to make sure that the else
            // statement matches the right if statement after the edit.
            var newIfNodeStatement = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement 
                ? SyntaxFactory.Block(ifNode.Else.Statement) 
                : ifNode.Else.Statement;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var invertedIf = ifNode.WithCondition(Negate(ifNode.Condition, semanticModel, cancellationToken))
                .WithStatement(newIfNodeStatement)
                .WithElse(ifNode.Else.WithStatement(ifNode.Statement))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var result = root.ReplaceNode(ifNode, invertedIf);
            return document.WithSyntaxRoot(result);
        }

        /// <summary>
        /// Returns true if the binaryExpression consists of an expression that can never be negative, 
        /// such as length or unsigned numeric types, being compared to zero with greater than, 
        /// less than, or equals relational operator.
        /// </summary>
        private bool IsSpecialCaseBinaryExpression(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (binaryExpression.Kind())
            {
                case SyntaxKind.GreaterThanExpression when binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.EqualsExpression when binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression:
                    return CanSimplifyToLengthEqualsZeroExpression(
                        binaryExpression.Left,
                        (LiteralExpressionSyntax)binaryExpression.Right,
                        semanticModel,
                        cancellationToken);
                case SyntaxKind.LessThanExpression when binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.EqualsExpression when binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression:
                    return CanSimplifyToLengthEqualsZeroExpression(
                        binaryExpression.Right,
                        (LiteralExpressionSyntax)binaryExpression.Left,
                        semanticModel,
                        cancellationToken);
            }

            return false;
        }

        private bool CanSimplifyToLengthEqualsZeroExpression(
            ExpressionSyntax variableExpression,
            LiteralExpressionSyntax numericLiteralExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var numericValue = semanticModel.GetConstantValue(numericLiteralExpression, cancellationToken);
            if (numericValue.HasValue && numericValue.Value is 0)
            {
                var symbol = semanticModel.GetSymbolInfo(variableExpression, cancellationToken).Symbol;

                if (symbol != null && (symbol.Name == nameof(Array.Length) || symbol.Name == LongLength))
                {
                    var containingType = symbol.ContainingType;
                    if (containingType != null &&
                        (containingType.SpecialType == SpecialType.System_Array ||
                         containingType.SpecialType == SpecialType.System_String))
                    {
                        return true;
                    }
                }

                var typeInfo = semanticModel.GetTypeInfo(variableExpression, cancellationToken);
                if (typeInfo.Type != null)
                {
                    switch (typeInfo.Type.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            return true;
                    }
                }
            }

            return false;
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
                if (IsSpecialCaseBinaryExpression(binaryExpression, semanticModel, cancellationToken))
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
