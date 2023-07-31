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

        protected override StatementSyntax GetNewStatement(
            SourceText sourceText,
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            int wrappingLength,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            return statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(sourceText, objectCreation, wrappingLength, useCollectionExpression, matches));
        }

        private static ExpressionSyntax GetNewObjectCreation(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            int wrappingLength,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            return useCollectionExpression
                ? CreateCollectionExpression(objectCreation, matches, MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
                : CreateObjectInitializerExpression(objectCreation, matches);
        }

        private static BaseObjectCreationExpressionSyntax CreateObjectInitializerExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            var expressions = CreateElements(objectCreation, matches, static (_, e) => e);
            var withLineBreaks = AddLineBreaks(expressions, includeFinalLineBreak: true);
            return UseInitializerHelpers.GetNewObjectCreation(objectCreation, withLineBreaks);
        }

        private static CollectionExpressionSyntax CreateCollectionExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            bool makeMultiLine)
        {
            var elements = CreateElements<CollectionElementSyntax>(
                objectCreation, matches,
                static (match, expression) => match?.UseSpread is true ? SpreadElement(expression) : ExpressionElement(expression));

            if (makeMultiLine)
                elements = AddLineBreaks(elements, includeFinalLineBreak: false);

            return CollectionExpression(elements).WithTriviaFrom(objectCreation);
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

        private static bool MakeMultiLine(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            int wrappingLength)
        {
            // If it's already multiline, keep it that way.
            if (!sourceText.AreOnSameLine(objectCreation.GetFirstToken(), objectCreation.GetLastToken()))
                return true;

            foreach (var match in matches)
            {
                var expression = GetExpression(match);

                // If we have anything like: `new Dictionary<X,Y> { { A, B }, { C, D } }` then always make multiline.
                // Similarly, if we have `new Dictionary<X,Y> { [A] = B }`.
                if (expression is InitializerExpressionSyntax or AssignmentExpressionSyntax)
                    return true;

                // if any of the expressions we're adding are multiline, then make things multiline.
                if (!sourceText.AreOnSameLine(expression.GetFirstToken(), expression.GetLastToken()))
                    return true;
            }

            var totalLength = "{}".Length;
            foreach (var match in matches)
            {
                var expression = GetExpression(match);
                totalLength += expression.Span.Length;
                totalLength += ", ".Length;

                if (totalLength > wrappingLength)
                    return true;
            }

            return false;

            static ExpressionSyntax GetExpression(Match<StatementSyntax> match)
                => match.Statement switch
                {
                    ExpressionStatementSyntax expressionStatement => expressionStatement.Expression,
                    ForEachStatementSyntax foreachStatement => foreachStatement.Expression,
                    _ => throw ExceptionUtilities.Unreachable(),
                };
        }

        public static SeparatedSyntaxList<TNode> AddLineBreaks<TNode>(
            SeparatedSyntaxList<TNode> nodes, bool includeFinalLineBreak)
            where TNode : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            var nodeOrTokenList = nodes.GetWithSeparators();
            foreach (var item in nodeOrTokenList)
            {
                var addLineBreak = item.IsToken || (includeFinalLineBreak && item == nodeOrTokenList.Last());
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

        private static SeparatedSyntaxList<TElement> CreateElements<TElement>(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElement> createElement)
            where TElement : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(objectCreation, nodesAndTokens, createElement);

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var statement = match.Statement;

                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var trivia = statement.GetLeadingTrivia();
                    var leadingTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                    var semicolon = expressionStatement.SemicolonToken;
                    var trailingTrivia = semicolon.TrailingTrivia.Contains(static t => t.IsSingleOrMultiLineComment())
                        ? semicolon.TrailingTrivia
                        : default;

                    var expression = createElement(match, ConvertExpression(expressionStatement.Expression).WithoutTrivia()).WithLeadingTrivia(leadingTrivia);
                    if (i < matches.Length - 1)
                    {
                        nodesAndTokens.Add(expression);
                        nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithTrailingTrivia(trailingTrivia));
                    }
                    else
                    {
                        nodesAndTokens.Add(expression.WithTrailingTrivia(trailingTrivia));
                    }
                }
                else if (statement is ForEachStatementSyntax foreachStatement)
                {
                    nodesAndTokens.Add(createElement(match, foreachStatement.Expression.WithoutTrivia()));
                    if (i < matches.Length - 1)
                        nodesAndTokens.Add(Token(SyntaxKind.CommaToken));
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            return SeparatedList<TElement>(nodesAndTokens);
        }
    }
}
