// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal partial class CSharpUseCollectionInitializerCodeFixProvider
{
    private static BaseObjectCreationExpressionSyntax CreateObjectInitializerExpression(
        BaseObjectCreationExpressionSyntax objectCreation,
        ImmutableArray<Match<StatementSyntax>> matches)
    {
        var expressions = CreateCollectionInitializerExpressions();
        var withLineBreaks = AddLineBreaks(expressions);

        var newCreation = UseInitializerHelpers.GetNewObjectCreation(objectCreation, withLineBreaks);
        return newCreation.WithAdditionalAnnotations(Formatter.Annotation);

        SeparatedSyntaxList<ExpressionSyntax> CreateCollectionInitializerExpressions()
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems<Match<StatementSyntax>, ExpressionSyntax>(
                objectCreation, nodesAndTokens, addTrailingComma: matches.Length > 0, static (_, expression) => expression);

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var statement = (ExpressionStatementSyntax)match.Statement;

                var trivia = statement.GetLeadingTrivia();
                var leadingTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                var trailingTrivia = statement.SemicolonToken.TrailingTrivia.Contains(static t => t.IsSingleOrMultiLineComment())
                    ? statement.SemicolonToken.TrailingTrivia
                    : default;

                var expression = ConvertExpression(statement.Expression)
                    .WithTrailingTrivia()
                    .WithLeadingTrivia(leadingTrivia);

                if (i < matches.Length - 1)
                {
                    nodesAndTokens.Add(expression);
                    nodesAndTokens.Add(CommaToken.WithTrailingTrivia(trailingTrivia));
                }
                else
                {
                    nodesAndTokens.Add(expression.WithTrailingTrivia(trailingTrivia));
                }
            }

            return SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }

        static SeparatedSyntaxList<TNode> AddLineBreaks<TNode>(SeparatedSyntaxList<TNode> nodes)
            where TNode : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            var nodeOrTokenList = nodes.GetWithSeparators();
            foreach (var item in nodeOrTokenList)
            {
                var addLineBreak = item.IsToken || item == nodeOrTokenList.Last();
                if (addLineBreak && item.GetTrailingTrivia() is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
                {
                    nodesAndTokens.Add(item.WithAppendedTrailingTrivia(ElasticCarriageReturnLineFeed));
                }
                else
                {
                    nodesAndTokens.Add(item);
                }
            }

            return SeparatedList<TNode>(nodesAndTokens);
        }

        static ExpressionSyntax ConvertExpression(
            ExpressionSyntax expression)
        {
            // This must be called from an expression from the original tree.  Not something we're already transforming.
            // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
            Contract.ThrowIfNull(expression.Parent);
            return expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment),
                _ => throw new InvalidOperationException(),
            };
        }

        static AssignmentExpressionSyntax ConvertAssignment(AssignmentExpressionSyntax assignment)
        {
            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            return assignment.WithLeft(
                ImplicitElementAccess(elementAccess.ArgumentList));
        }

        static ExpressionSyntax ConvertInvocation(InvocationExpressionSyntax invocation)
        {
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 1)
            {
                // Assignment expressions in a collection initializer will cause the compiler to report an error.
                // This is because { a = b } is the form for an object initializer, and the two forms are not
                // allowed to mix/match.  Parenthesize the assignment to avoid the ambiguity.
                //
                // Similarly, we cannot have `{ [...] }` in an object initializer (because it will be viewed as a
                // dictionary assignment).
                var expression = arguments[0].Expression;
                return expression is AssignmentExpressionSyntax or CollectionExpressionSyntax
                    ? ParenthesizedExpression(expression)
                    : expression;
            }
            else
            {
                return InitializerExpression(
                    SyntaxKind.ComplexElementInitializerExpression,
                    OpenBraceToken.WithoutTrivia(),
                    SeparatedList(
                        arguments.Select(a => a.Expression),
                        arguments.GetSeparators()),
                    CloseBraceToken.WithoutTrivia());
            }
        }
    }
}
