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
using Microsoft.CodeAnalysis.LanguageServices;
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
            Document document,
            SemanticModel model,
            SyntaxNode ifStatement,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = GetSyntaxFactsService();

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

            ifNode = ifNode.WithCondition((ExpressionSyntax)(Negate(ifNode.Condition, generator, syntaxFacts, model, cancellationToken)))
                .WithStatement(newIfStatment)
                .WithElse(newElseStatement);

            if (isMultiLine)
            {
                ifNode = ifNode.WithAdditionalAnnotations(Formatter.Annotation);
            }

            // get new root
            return model.SyntaxTree.GetRoot().ReplaceNode(ifStatement, ifNode);
        }

        private void GetPartsOfIfStatement(SyntaxNode node, out SyntaxNode condition, out SyntaxNode thenStatements, out SyntaxNode elseStatements)
        {
            var ifStatement = (IfStatementSyntax)node;
            condition = ifStatement.Condition;
            thenStatements = ifStatement.Statement;
            elseStatements = ifStatement.Else;
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
           => CSharpSyntaxFactsService.Instance;

        internal override string GetInvertIfText()
        {
            return CSharpFeaturesResources.Invert_if;
        }

        internal override IOperation GetBinaryOperation(SyntaxNode expressionNode, SemanticModel semanticModel)
        {
            return semanticModel.GetOperation((BinaryExpressionSyntax)expressionNode);
        }

        internal override bool IsConditionalAnd(IBinaryOperation binaryOperation)
        {
            return binaryOperation.Syntax.ToString().Contains("&&");
        }

        internal override bool IsConditionalOr(IBinaryOperation binaryOperation)
        {
            return binaryOperation.Syntax.ToString().Contains("||");
        }
    }
}
