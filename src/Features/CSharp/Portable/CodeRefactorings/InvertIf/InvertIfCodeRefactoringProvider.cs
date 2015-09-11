// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    // [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf)]
    internal partial class InvertIfCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly Dictionary<SyntaxKind, Tuple<SyntaxKind, SyntaxKind>> s_binaryMap =
            new Dictionary<SyntaxKind, Tuple<SyntaxKind, SyntaxKind>>(SyntaxFacts.EqualityComparer)
                {
                    { SyntaxKind.EqualsExpression, Tuple.Create(SyntaxKind.NotEqualsExpression, SyntaxKind.ExclamationEqualsToken) },
                    { SyntaxKind.NotEqualsExpression, Tuple.Create(SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken) },
                    { SyntaxKind.LessThanExpression, Tuple.Create(SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken) },
                    { SyntaxKind.LessThanOrEqualExpression, Tuple.Create(SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken) },
                    { SyntaxKind.GreaterThanExpression, Tuple.Create(SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken) },
                    { SyntaxKind.GreaterThanOrEqualExpression, Tuple.Create(SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken) },
                };

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
                    CSharpFeaturesResources.InvertIfStatement,
                    (c) => InvertIfAsync(document, ifStatement, c)));
        }

        private async Task<Document> InvertIfAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var ifNode = ifStatement;

            // In the case that the else clause is actually an else if clause, place the if
            // statement to be moved in a new block in order to make sure that the else
            // statement matches the right if statement after the edit.
            var newIfNodeStatement = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement ?
                SyntaxFactory.Block(ifNode.Else.Statement) :
                ifNode.Else.Statement;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var invertedIf = ifNode.WithCondition(Negate(ifNode.Condition, semanticModel, cancellationToken))
                      .WithStatement(newIfNodeStatement)
                      .WithElse(ifNode.Else.WithStatement(ifNode.Statement))
                      .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var result = root.ReplaceNode(ifNode, invertedIf);
            return document.WithSyntaxRoot(result);
        }

        private bool IsComparisonOfZeroAndSomethingNeverLessThanZero(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var canSimplify = false;

            if (binaryExpression.Kind() == SyntaxKind.GreaterThanExpression &&
                binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Left,
                    (LiteralExpressionSyntax)binaryExpression.Right,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.LessThanExpression &&
                     binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Right,
                    (LiteralExpressionSyntax)binaryExpression.Left,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                     binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Left,
                    (LiteralExpressionSyntax)binaryExpression.Right,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                     binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Right,
                    (LiteralExpressionSyntax)binaryExpression.Left,
                    semanticModel,
                    cancellationToken);
            }

            return canSimplify;
        }

        private bool CanSimplifyToLengthEqualsZeroExpression(
            ExpressionSyntax variableExpression,
            LiteralExpressionSyntax numericLiteralExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var numericValue = semanticModel.GetConstantValue(numericLiteralExpression, cancellationToken);
            if (numericValue.HasValue && numericValue.Value is int && (int)numericValue.Value == 0)
            {
                var symbol = semanticModel.GetSymbolInfo(variableExpression, cancellationToken).Symbol;

                if (symbol != null && (symbol.Name == "Length" || symbol.Name == "LongLength"))
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
            Tuple<SyntaxKind, SyntaxKind> tuple;
            if (s_binaryMap.TryGetValue(expression.Kind(), out tuple))
            {
                var binaryExpression = (BinaryExpressionSyntax)expression;
                var expressionType = tuple.Item1;
                var operatorType = tuple.Item2;

                // Special case negating Length > 0 to Length == 0 and 0 < Length to 0 == Length
                // for arrays and strings. We can do this because we know that Length cannot be
                // less than 0. Additionally, if we find Length == 0 or 0 == Length, we'll invert
                // it to Length > 0 or 0 < Length, respectively.
                if (IsComparisonOfZeroAndSomethingNeverLessThanZero(binaryExpression, semanticModel, cancellationToken))
                {
                    operatorType = binaryExpression.OperatorToken.Kind() == SyntaxKind.EqualsEqualsToken
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanToken : SyntaxKind.LessThanToken
                        : SyntaxKind.EqualsEqualsToken;
                    expressionType = binaryExpression.Kind() == SyntaxKind.EqualsExpression
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanExpression : SyntaxKind.LessThanExpression
                        : SyntaxKind.EqualsExpression;
                }

                result = SyntaxFactory.BinaryExpression(
                    expressionType,
                    binaryExpression.Left,
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        operatorType,
                        binaryExpression.OperatorToken.TrailingTrivia),
                    binaryExpression.Right);

                return true;
            }

            result = null;
            return false;
        }

        private ExpressionSyntax Negate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            ExpressionSyntax result;
            if (TryNegateBinaryComparisonExpression(expression, semanticModel, cancellationToken, out result))
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
