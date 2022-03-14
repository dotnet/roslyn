// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
    internal class CSharpUseCollectionInitializerCodeFixProvider :
        AbstractUseCollectionInitializerCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            BaseObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseCollectionInitializerCodeFixProvider()
        {
        }

        protected override StatementSyntax GetNewStatement(
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches));
        }

        private static BaseObjectCreationExpressionSyntax GetNewObjectCreation(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation, CreateExpressions(objectCreation, matches));
        }

        private static SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens);

            for (var i = 0; i < matches.Length; i++)
            {
                var expressionStatement = matches[i];
                var trivia = expressionStatement.GetLeadingTrivia();

                var newTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                var newExpression = ConvertExpression(expressionStatement.Expression)
                    .WithoutTrivia()
                    .WithPrependedLeadingTrivia(newTrivia);

                if (i < matches.Length - 1)
                {
                    nodesAndTokens.Add(newExpression);
                    var commaToken = SyntaxFactory.Token(SyntaxKind.CommaToken)
                        .WithTriviaFrom(expressionStatement.SemicolonToken);

                    nodesAndTokens.Add(commaToken);
                }
                else
                {
                    newExpression = newExpression.WithTrailingTrivia(
                        expressionStatement.GetTrailingTrivia());
                    nodesAndTokens.Add(newExpression);
                }
            }

            return SyntaxFactory.SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }

        private static ExpressionSyntax ConvertExpression(ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                return ConvertInvocation(invocation);
            }
            else if (expression is AssignmentExpressionSyntax assignment)
            {
                return ConvertAssignment(assignment);
            }

            throw new InvalidOperationException();
        }

        private static ExpressionSyntax ConvertAssignment(AssignmentExpressionSyntax assignment)
        {
            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            return assignment.WithLeft(
                SyntaxFactory.ImplicitElementAccess(elementAccess.ArgumentList));
        }

        private static ExpressionSyntax ConvertInvocation(InvocationExpressionSyntax invocation)
        {
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 1)
            {
                // Assignment expressions in a collection initializer will cause the compiler to 
                // report an error.  This is because { a = b } is the form for an object initializer,
                // and the two forms are not allowed to mix/match.  Parenthesize the assignment to
                // avoid the ambiguity.
                var expression = arguments[0].Expression;
                return SyntaxFacts.IsAssignmentExpression(expression.Kind())
                    ? SyntaxFactory.ParenthesizedExpression(expression)
                    : expression;
            }

            return SyntaxFactory.InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithoutTrivia(),
                SyntaxFactory.SeparatedList(
                    arguments.Select(a => a.Expression),
                    arguments.GetSeparators()),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithoutTrivia());
        }
    }
}
