// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseObjectInitializer), Shared]
    internal class CSharpUseObjectInitializerCodeFixProvider :
        AbstractUseObjectInitializerCodeFixProvider<
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        protected override ObjectCreationExpressionSyntax GetNewObjectCreation(
            DocumentOptionSet options,
            ObjectCreationExpressionSyntax objectCreation,
            List<Match<ExpressionStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax>> matches)
        {
            var openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                         .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            var initializer = SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                CreateExpressions(matches)).WithOpenBraceToken(openBrace);

            return objectCreation.WithInitializer(initializer)
                                 .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            List<Match<ExpressionStatementSyntax, MemberAccessExpressionSyntax, ExpressionSyntax>> matches)
        {
            var nodesAndTokens = new List<SyntaxNodeOrToken>();
            foreach (var match in matches)
            {
                var expressionStatement = match.Statement;
                var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;

                var newAssignment = assignment.WithLeft(
                    match.MemberAccessExpression.Name.WithLeadingTrivia(match.MemberAccessExpression.GetLeadingTrivia()));

                nodesAndTokens.Add(newAssignment);
                var commaToken = SyntaxFactory.Token(SyntaxKind.CommaToken)
                    .WithTriviaFrom(expressionStatement.SemicolonToken);

                nodesAndTokens.Add(commaToken);
            }

            return SyntaxFactory.SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }
    }
}