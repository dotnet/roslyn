// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseObjectInitializer), Shared]
    internal class CSharpUseObjectInitializerCodeFixProvider :
        AbstractUseObjectInitializerCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        [ImportingConstructor]
        public CSharpUseObjectInitializerCodeFixProvider()
        {
        }

        protected override StatementSyntax GetNewStatement(
            StatementSyntax statement, ObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches));
        }

        private ObjectCreationExpressionSyntax GetNewObjectCreation(
            ObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation, CreateExpressions(matches));
        }

        private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            var nodesAndTokens = new List<SyntaxNodeOrToken>();
            for (var i = 0; i < matches.Length; i++)
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
