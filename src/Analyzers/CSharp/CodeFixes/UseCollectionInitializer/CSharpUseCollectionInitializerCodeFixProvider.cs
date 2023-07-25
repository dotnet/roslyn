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
            var expressions = CreateExpressions(objectCreation, matches);
            if (!useCollectionExpression || MakeMultiLine(sourceText, objectCreation, expressions, wrappingLength))
                expressions = AddLineBreaks(expressions);

            return useCollectionExpression
                ? CreateCollectionExpression(objectCreation, matches, expressions)
                : UseInitializerHelpers.GetNewObjectCreation(objectCreation, expressions);
        }

        private static SeparatedSyntaxList<ExpressionSyntax> AddLineBreaks(SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            foreach (var item in expressions.GetWithSeparators())
            {
                if (item.IsNode)
                {
                    var expression = item.AsNode()!;
                    if (expression == expressions.Last() &&
                        expressions.SeparatorCount < expressions.Count &&
                        expression.GetTrailingTrivia() is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
                    {
                        expression = expression.WithAppendedTrailingTrivia(ElasticCarriageReturnLineFeed);
                    }

                    nodesAndTokens.Add(expression);
                }
                else
                {
                    var token = item.AsToken();
                    if (token.TrailingTrivia is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
                        token = token.WithAppendedTrailingTrivia(ElasticCarriageReturnLineFeed);

                    nodesAndTokens.Add(token);
                }
            }

            return SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }

        private static bool MakeMultiLine(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            SeparatedSyntaxList<ExpressionSyntax> expressions,
            int wrappingLength)
        {
            // If we have anything like: `new Dictionary<X,Y> { { A, B }, { C, D } }` the always make multiline.
            // Similarly, if we have `new Dictionary<X,Y> { [A] = B }`.

            foreach (var expression in expressions)
            {
                if (expression is InitializerExpressionSyntax or AssignmentExpressionSyntax)
                    return true;
            }

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

                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var semicolon = expressionStatement.SemicolonToken;
                    var trailingTrivia = semicolon.TrailingTrivia.Contains(static t => t.IsSingleOrMultiLineComment())
                        ? TriviaList(semicolon.TrailingTrivia.TakeWhile(static t => t.Kind() != SyntaxKind.EndOfLineTrivia))
                        : default;

                    var expression = ConvertExpression(expressionStatement.Expression).WithoutTrivia();
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
                    nodesAndTokens.Add(foreachStatement.Expression.WithoutTrivia());
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
