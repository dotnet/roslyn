// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseObjectInitializer), Shared]
    internal class CSharpUseObjectInitializerCodeFixProvider :
        AbstractUseObjectInitializerCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            BaseObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseObjectInitializerCodeFixProvider()
        {
        }

        protected override StatementSyntax GetNewStatement(
            StatementSyntax statement, BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches));
        }

        private static BaseObjectCreationExpressionSyntax GetNewObjectCreation(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation, CreateExpressions(objectCreation, matches));
        }

        private static SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
                BaseObjectCreationExpressionSyntax objectCreation,
                ImmutableArray<Match<ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, ExpressionStatementSyntax>> matches)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens);

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var expressionStatement = match.Statement;
                var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
                var trivia = match.MemberAccessExpression.GetLeadingTrivia();

                var newTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                var newAssignment = assignment.WithLeft(
                    match.MemberAccessExpression.Name.WithLeadingTrivia(newTrivia));

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
