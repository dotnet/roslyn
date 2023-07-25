// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

        protected override async Task<StatementSyntax> GetNewStatementAsync(
            SourceText sourceText,
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            int wrappingLength,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                await GetNewObjectCreationAsync(sourceText, objectCreation, wrappingLength, useCollectionExpression, matches).ConfigureAwait(false));
        }

        private static async Task<ExpressionSyntax> GetNewObjectCreationAsync(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            int wrappingLength,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            var expressions = CreateExpressions(objectCreation, matches);
            if (MakeMultiLine(sourceText, objectCreation, expressions, wrappingLength))
                expressions = AddLineBreaks(expressions);

            return useCollectionExpression
                ? CreateCollectionExpression(objectCreation, matches, expressions)
                : UseInitializerHelpers.GetNewObjectCreation(objectCreation, expressions);
        }

        private static bool MakeMultiLine(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            SeparatedSyntaxList<ExpressionSyntax> expressions,
            int wrappingLength)
        {
            // If it's already multiline, keep it that way.
            if (!sourceText.AreOnSameLine(objectCreation.GetFirstToken(), objectCreation.GetLastToken()))
                return true;

            // if any of the expressions we're adding are multiline, then make things multiline.
            foreach (var expression in expressions)
            {
                if (!sourceText.AreOnSameLine(expression.GetFirstToken(), expression.GetLastToken()))
                    return true;
            }

            var totalLength = 2; // for the braces.
            foreach (var item in expressions.GetWithSeparators())
            {
                totalLength += item.Span.Length;

                // add a space for after each comma.
                if (item.IsToken)
                    totalLength++;

                if (totalLength > wrappingLength)
                    return true;
            }

            return false;
        }

        private static CollectionExpressionSyntax CreateCollectionExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            // 'expressions' is the entire list of expressions that will go into the collection expression. some will be
            // new, but some may be from 

            var expressionIndex = 0;
            var expressionOffset = expressions.Count - matches.Length;
            foreach (var nodeOrToken in expressions.GetWithSeparators())
            {
                if (nodeOrToken.IsToken)
                {
                    nodesAndTokens.Add(nodeOrToken.AsToken());
                }
                else
                {
                    var expression = (ExpressionSyntax)nodeOrToken.AsNode()!;
                    nodesAndTokens.Add(expressionIndex < expressionOffset || !matches[expressionIndex - expressionOffset].UseSpread
                        ? ExpressionElement(expression)
                        : SpreadElement(expression));
                    expressionIndex++;
                }
            }

            return CollectionExpression(SeparatedList<CollectionElementSyntax>(nodesAndTokens)).WithTriviaFrom(objectCreation);
        }

        private static SeparatedSyntaxList<ExpressionSyntax> CreateExpressions(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens);

            for (var i = 0; i < matches.Length; i++)
            {
                var statement = matches[i].Statement;

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
                    var newExpression = foreachStatement.Expression
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
            => expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment),
                _ => throw new InvalidOperationException(),
            };

        private static AssignmentExpressionSyntax ConvertAssignment(AssignmentExpressionSyntax assignment)
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
