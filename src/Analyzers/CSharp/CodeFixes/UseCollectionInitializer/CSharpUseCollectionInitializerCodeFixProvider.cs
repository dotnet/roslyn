// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
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
            IfStatementSyntax,
            VariableDeclaratorSyntax,
            CSharpUseCollectionInitializerAnalyzer>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseCollectionInitializerCodeFixProvider()
        {
        }

        protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
            => CSharpUseCollectionInitializerAnalyzer.Allocate();

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
                ? CreateCollectionExpression(sourceText, objectCreation, wrappingLength, matches)
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
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            int wrappingLength,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            var elements = CreateElements<CollectionElementSyntax>(
                objectCreation, matches, CreateCollectionElement);

            //if (MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
            //    elements = AddLineBreaks(elements, includeFinalLineBreak: false);

            // If the object creation expression had an initializer.  Attempt to preserve the formatting of the original
            // initializer and the new collection expression.
            if (objectCreation.Initializer != null)
            {
                if (MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // Both the initializer and the new elements all would work on a single line.

                    // First, convert the existing initializer (and its expressions) into a corresponding collection
                    // expression.  This will fixup the braces properly for the collection expression.
                    var initialConversion = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        objectCreation.Initializer, wasOnSingleLine: true);

                    // now, add all the matches in after the existing elements.
                    var totalConversion = initialConversion.AddElements(
                        matches.Select(m => CreateElement(m, CreateCollectionElement)).ToArray());

                    // Now do the actual replacement.  This will ensure the location of the collection expression
                    // properly corresponds to the equivalent pieces of the collection initializer.
                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        sourceText, objectCreation.Initializer, totalConversion);
                }
            }
            else
            {
                // Didn't have an existing initializer.

                if (MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // 
                }
            }

            return CollectionExpression(elements);

            static CollectionElementSyntax CreateCollectionElement(
                Match<StatementSyntax>? match,
                ExpressionSyntax expression)
            {
                return match?.UseSpread is true ? SpreadElement(expression) : ExpressionElement(expression);
            }
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
            // If the existing creation expression already has an initializer, and that initializer is already
            // multiline, then keep things that way.
            if (objectCreation.Initializer != null && !sourceText.AreOnSameLine(objectCreation.Initializer.GetFirstToken(), objectCreation.Initializer.GetLastToken()))
                return true;

            var totalLength = "{}".Length;
            foreach (var (statement, _) in matches)
            {
                // if the statement we're replacing has any comments on it, then we need to be multiline to give them an
                // appropriate place to go.
                if (statement.GetLeadingTrivia().Any(static t => t.IsSingleOrMultiLineComment()) ||
                    statement.GetTrailingTrivia().Any(static t => t.IsSingleOrMultiLineComment()))
                {
                    return true;
                }

                foreach (var component in GetElementComponents(statement))
                {
                    // if any of the expressions we're adding are multiline, then make things multiline.
                    if (!sourceText.AreOnSameLine(component.GetFirstToken(), component.GetLastToken()))
                        return true;

                    totalLength += component.Span.Length;
                    totalLength += ", ".Length;

                    if (totalLength > wrappingLength)
                        return true;
                }
            }

            return false;

            static IEnumerable<SyntaxNode> GetElementComponents(StatementSyntax statement)
            {
                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    yield return expressionStatement.Expression;
                }
                else if (statement is ForEachStatementSyntax foreachStatement)
                {
                    yield return foreachStatement.Expression;
                }
                else if (statement is IfStatementSyntax ifStatement)
                {
                    yield return ifStatement.Condition;
                    yield return UnwrapEmbeddedStatement(ifStatement.Statement);
                    if (ifStatement.Else != null)
                        yield return UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                }
            }
        }

        private static StatementSyntax UnwrapEmbeddedStatement(StatementSyntax statement)
            => statement is BlockSyntax { Statements: [var innerStatement] } ? innerStatement : statement;

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

        private static TElementSyntax CreateElement<TElementSyntax>(
            Match<StatementSyntax> match,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElementSyntax> createElement)
            where TElementSyntax : SyntaxNode
        {
            var statement = match.Statement;

            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                return createElement(match, ConvertExpression(expressionStatement.Expression).WithoutTrivia());
            }
            else if (statement is ForEachStatementSyntax foreachStatement)
            {
                return createElement(match, foreachStatement.Expression.WithoutTrivia());
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                var trueStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Statement);

                if (ifStatement.Else is null)
                {
                    // Create: x ? [y] : []
                    var expression = ConditionalExpression(
                        ifStatement.Condition.Parenthesize(),
                        CollectionExpression(SingletonSeparatedList<CollectionElementSyntax>(ExpressionElement(ConvertExpression(trueStatement.Expression)))),
                        CollectionExpression());
                    return createElement(match, expression);
                }
                else
                {
                    // Create: x ? y : z
                    var falseStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                    var expression = ConditionalExpression(ifStatement.Condition.Parenthesize(), ConvertExpression(trueStatement.Expression), ConvertExpression(falseStatement.Expression));
                    return createElement(match, expression);
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static SeparatedSyntaxList<TElement> CreateElements<TElement>(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElement> createElement)
            where TElement : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(
                objectCreation, nodesAndTokens, addTrailingComma: matches.Length > 0, createElement);

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var statement = match.Statement;

                var element = CreateElement(match, createElement);

                var trivia = statement.GetLeadingTrivia();
                var leadingTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                var semicolon = statement is ExpressionStatementSyntax expressionStatement ? expressionStatement.SemicolonToken : default;
                var trailingTrivia = semicolon.TrailingTrivia.Contains(static t => t.IsSingleOrMultiLineComment())
                    ? semicolon.TrailingTrivia
                    : default;

                if (i < matches.Length - 1)
                {
                    nodesAndTokens.Add(element);
                    nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithTrailingTrivia(trailingTrivia));
                }
                else
                {
                    nodesAndTokens.Add(element.WithTrailingTrivia(trailingTrivia));
                }
            }

            return SeparatedList<TElement>(nodesAndTokens);
        }
    }
}
