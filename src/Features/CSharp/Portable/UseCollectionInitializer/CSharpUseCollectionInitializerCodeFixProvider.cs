// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            return objectCreation.WithInitializer(initializer);
        }

        private SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            ImmutableArray<ExpressionStatementSyntax> matches)
        {
            var nodesAndTokens = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < matches.Length; i++)
            {
                var expressionStatement = matches[i];
                var invocation = (InvocationExpressionSyntax)expressionStatement.Expression;

                var arguments = invocation.ArgumentList.Arguments;
                ExpressionSyntax newExpression;
                if (arguments.Count == 1)
                {
                    newExpression = arguments[0].Expression.WithoutTrivia();
                }
                else
                {
                    newExpression = SyntaxFactory.InitializerExpression(
                        SyntaxKind.ComplexElementInitializerExpression,
                        SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.OpenBraceToken, default(SyntaxTriviaList)),
                        SyntaxFactory.SeparatedList(
                            arguments.Select(a => a.Expression),
                            arguments.GetSeparators()),
                        SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.CloseBraceToken, default(SyntaxTriviaList)));
                }

                newExpression = newExpression.WithLeadingTrivia(expressionStatement.GetLeadingTrivia());
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
    }
}