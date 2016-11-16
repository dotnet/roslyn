// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseObjectInitializerDiagnosticAnalyzer :
        AbstractUseObjectInitializerDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        protected override bool FadeOutOperatorToken => true;

        protected override bool AreObjectInitializersSupported(SyntaxNodeAnalysisContext context)
        {
            // object initializers are only available in C# 3.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            return ((CSharpParseOptions)context.Node.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp3;
        }

        protected override SyntaxKind GetObjectCreationSyntaxKind() => SyntaxKind.ObjectCreationExpression;

        protected override ISyntaxFactsService GetSyntaxFactsService() => CSharpSyntaxFactsService.Instance;

        protected override ObjectCreationExpressionSyntax GetNewObjectCreation(
            ObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match> matches)
        {
            var openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                         .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            var initializer = SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                CreateExpressions(matches)).WithOpenBraceToken(openBrace);

            return objectCreation.WithInitializer(initializer);
        }

        private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            ImmutableArray<Match> matches)
        {
            var nodesAndTokens = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var expressionStatement = match.Statement;
                var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;

                var newAssignment = assignment.WithLeft(
                    match.MemberAccessExpression.Name.WithLeadingTrivia(match.MemberAccessExpression.GetLeadingTrivia()));

                if (i < matches.Length - 1)
                {
                    nodesAndTokens.Add(newAssignment);
                    var commaToken = SyntaxFactory.Token(SyntaxKind.CommaToken)
                        .WithTriviaFrom(expressionStatement.SemicolonToken);

                    nodesAndTokens.Add(commaToken);
                }
                else
                {
                    newAssignment = newAssignment.WithTrailingTrivia(
                        expressionStatement.GetTrailingTrivia());
                    nodesAndTokens.Add(newAssignment);
                }
            }

            return SyntaxFactory.SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }
    }
}