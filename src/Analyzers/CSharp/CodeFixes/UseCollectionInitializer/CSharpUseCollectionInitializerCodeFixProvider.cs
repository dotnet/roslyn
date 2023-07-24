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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    using static SyntaxFactory;

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
            ForEachStatementSyntax,
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
            bool useCollectionExpression,
            ImmutableArray<StatementSyntax> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, useCollectionExpression, matches));
        }

        private static ExpressionSyntax GetNewObjectCreation(
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<StatementSyntax> matches)
        {
            var expressions = CreateExpressions(objectCreation, matches);
            return useCollectionExpression
                ? CreateCollectionExpression(objectCreation, matches, expressions)
                : UseInitializerHelpers.GetNewObjectCreation(objectCreation, expressions);
        }

        private static CollectionExpressionSyntax CreateCollectionExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<StatementSyntax> matches,
            SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            var expressionIndex = 0;
            foreach (var nodeOrToken in expressions.GetWithSeparators())
            {
                if (nodeOrToken.IsToken)
                {
                    nodesAndTokens.Add(nodeOrToken.AsToken());
                }
                else
                {
                    var expression = (ExpressionSyntax)nodeOrToken.AsNode()!;
                    nodesAndTokens.Add(matches[expressionIndex] is ForEachStatementSyntax
                        ? SpreadElement(expression)
                        : ExpressionElement(expression));
                    expressionIndex++;
                }
            }

            return CollectionExpression(SeparatedList<CollectionElementSyntax>(nodesAndTokens)).WithTriviaFrom(objectCreation);
        }

        private static SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<StatementSyntax> matches)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens);

            for (var i = 0; i < matches.Length; i++)
            {
                var statement = matches[i];

                var trivia = statement.GetLeadingTrivia();
                var newTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var newExpression = ConvertExpression(expressionStatement.Expression)
                        .WithoutTrivia()
                        .WithPrependedLeadingTrivia(newTrivia);

                    if (i < matches.Length - 1)
                    {
                        nodesAndTokens.Add(newExpression);
                        var commaToken = Token(SyntaxKind.CommaToken)
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
                else if (statement is ForEachStatementSyntax foreachStatement)
                {
                    var newExpression = ConvertExpression(foreachStatement.Expression)
                        .WithoutTrivia()
                        .WithPrependedLeadingTrivia(newTrivia);
                    nodesAndTokens.Add(newExpression);
                    if (i < matches.Length - 1)
                        nodesAndTokens.Add(Token(SyntaxKind.CommaToken));
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            return SeparatedList<ExpressionSyntax>(nodesAndTokens);
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
                ImplicitElementAccess(elementAccess.ArgumentList));
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
                    ? ParenthesizedExpression(expression)
                    : expression;
            }

            return InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                Token(SyntaxKind.OpenBraceToken).WithoutTrivia(),
                SeparatedList(
                    arguments.Select(a => a.Expression),
                    arguments.GetSeparators()),
                Token(SyntaxKind.CloseBraceToken).WithoutTrivia());
        }
    }
}
