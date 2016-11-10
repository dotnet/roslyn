// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
    internal class CSharpUseCollectionInitializerCodeFixProvider :
        AbstractUseCollectionInitializerCodeFixProvider<
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        protected override ObjectCreationExpressionSyntax GetNewObjectCreation(
            ObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            var openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                         .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            var initializer = SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                CreateExpressions(matches)).WithOpenBraceToken(openBrace);

            if (objectCreation.ArgumentList != null &&
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                objectCreation = objectCreation.WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia()))
                                               .WithArgumentList(null);
            }

            return objectCreation.WithInitializer(initializer);
        }

        private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            var nodesAndTokens = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < matches.Length; i++)
            {
                var expressionStatement = matches[i];

                var newExpression = ConvertExpression(expressionStatement.Expression)
                    .WithoutTrivia()
                    .WithLeadingTrivia(expressionStatement.GetLeadingTrivia());

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
            if (expression is InvocationExpressionSyntax)
            {
                return ConvertInvocation((InvocationExpressionSyntax)expression);
            }
            else if (expression is AssignmentExpressionSyntax)
            {
                return ConvertAssignment((AssignmentExpressionSyntax)expression);
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
                return arguments[0].Expression;
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