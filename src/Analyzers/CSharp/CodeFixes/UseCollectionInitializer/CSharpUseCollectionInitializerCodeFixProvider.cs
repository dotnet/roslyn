// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
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

        protected override async Task<StatementSyntax> GetNewStatementAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            var newObjectCreation = await GetNewObjectCreationAsync(
                document, fallbackOptions, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);
            return statement.ReplaceNode(objectCreation, newObjectCreation);
        }

        private static async Task<ExpressionSyntax> GetNewObjectCreationAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            return useCollectionExpression
                ? await CreateCollectionExpressionAsync(document, fallbackOptions, objectCreation, matches, cancellationToken).ConfigureAwait(false)
                : CreateObjectInitializerExpression(objectCreation, matches);
        }

        private static BaseObjectCreationExpressionSyntax CreateObjectInitializerExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            var expressions = CreateCollectionInitializerExpressions(objectCreation, matches);
            var withLineBreaks = AddLineBreaks(expressions);
            var newCreation = UseInitializerHelpers.GetNewObjectCreation(objectCreation, withLineBreaks);
            return newCreation.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            //if (MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
            //    elements = AddLineBreaks(elements, includeFinalLineBreak: false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

#if CODE_STYLE
            var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
#else
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(
                fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

            var indentationOptions = new IndentationOptions(formattingOptions);

            // the option is currently not an editorconfig option, so not available in code style layer
            var wrappingLength =
#if !CODE_STYLE
                fallbackOptions.GetOptions(document.Project.Services)?.CollectionExpressionWrappingLength ??
#endif
                CodeActionOptions.DefaultCollectionExpressionWrappingLength;

            var makeMultiLine = MakeMultiLine(sourceText, objectCreation, matches, wrappingLength);

            var initializer = objectCreation.Initializer;
            return initializer == null || initializer.Expressions.Count == 0
                ? CreateCollectionExpressionWithoutExistingElements()
                : CreateCollectionExpressionWithExistingElements();

            CollectionExpressionSyntax CreateCollectionExpressionWithoutExistingElements()
            {
                // Didn't have an existing initializer (or it was empty).  For both cases, just create an entirely
                // fresh collection expression, and replace the object entirely.
                if (makeMultiLine)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
                    // fresh collection expression, and do a wholesale replacement of the original object creation
                    // expression with it.
                    var collectionExpression = CollectionExpression(
                        SeparatedList(matches.Select(m => CreateElement(m, CreateCollectionElement, preferredIndentation: null))));
                    return collectionExpression.WithTriviaFrom(objectCreation);
                }
            }

            CollectionExpressionSyntax CreateCollectionExpressionWithExistingElements()
            {
                // If the object creation expression had an initializer (with at least one element in it).  Attempt to
                // preserve the formatting of the original initializer and the new collection expression.
                var initializer = objectCreation.Initializer;
                Contract.ThrowIfNull(initializer);

                var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
                var indentationOptions = new IndentationOptions(formattingOptions);

                if (!sourceText.AreOnSameLine(initializer.GetFirstToken(), initializer.GetLastToken()))
                {
                    // initializer itself was on multiple lines.  We'll want to create a collection expression whose
                    // braces (and initial elements) match whatever the initializer correct looks like.

                    var initialConversion = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (!makeMultiLine &&
                        sourceText.AreOnSameLine(initializer.Expressions.First().GetFirstToken(), initializer.Expressions.Last().GetLastToken()))
                    {
                        // New elements were all single line, and existing elements were on a single line.  e.g.
                        //
                        //  {
                        //      1, 2, 3
                        //  }
                        //
                        // Just add the new elements to this.
                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialConversion, preferredIndentation: null);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection);
                    }
                    else
                    {
                        // We want the new items to be multiline *or* existing items were on different lines already.
                        // Figure out what the preferred indentation is, and prepend each new item with it.
                        var preferredIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(
                            parsedDocument, indentationOptions, cancellationToken);

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialConversion, preferredIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection);
                    }
                }
                else if (makeMultiLine)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // Both the initializer and the new elements all would work on a single line.

                    // First, convert the existing initializer (and its expressions) into a corresponding collection
                    // expression.  This will fixup the braces properly for the collection expression.
                    var initialConversion = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: true);

                    // now, add all the matches in after the existing elements.
                    var totalConversion = AddMatchesToExistingCollectionExpression(
                        initialConversion, preferredIndentation: null);

                    // Now do the actual replacement.  This will ensure the location of the collection expression
                    // properly corresponds to the equivalent pieces of the collection initializer.
                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        sourceText, initializer, totalConversion);
                }
            }

            CollectionExpressionSyntax AddMatchesToExistingCollectionExpression(
                CollectionExpressionSyntax initialCollectionExpression,
                string? preferredIndentation)
            {
                var triviaAfterComma = preferredIndentation is null
                    ? TriviaList(Space)
                    : TriviaList(EndOfLine(formattingOptions.NewLine));
                var commaToken = Token(SyntaxKind.CommaToken).WithoutLeadingTrivia().WithTrailingTrivia(triviaAfterComma);

                //triviaAfterComma: TriviaList(Space),
                //                            triviaBeforeElement: default);

                //triviaAfterComma: TriviaList(EndOfLine(formattingOptions.NewLine)),
                //                            triviaBeforeElement: TriviaList(Whitespace(preferredIndentation)));

                using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                nodesAndTokens.AddRange(initialCollectionExpression.Elements.GetWithSeparators());

                // If there is already a trailing comma before, remove it.  We'll add it back at the end.
                // If there is no trailing comma, then grab the trailing trivia off of the last element.
                // We'll move it to the final last element once we've added everything.
                var trailingComma = default(SyntaxToken);
                var trailingTrivia = default(SyntaxTriviaList);
                if (nodesAndTokens[^1].IsToken)
                {
                    trailingComma = nodesAndTokens[^1].AsToken();
                    nodesAndTokens.RemoveLast();
                }
                else
                {
                    trailingTrivia = nodesAndTokens[^1].GetTrailingTrivia();
                    nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia();
                }

                foreach (var element in matches.Select(m => CreateElement(m, CreateCollectionElement, preferredIndentation)))
                {
                    // Add a comment before each new element we're adding.
                    nodesAndTokens.Add(commaToken);
                    nodesAndTokens.Add(element);
                }

                if (trailingComma != default)
                {
                    // If we ended with a comma before, continue ending with a comma.
                    nodesAndTokens.Add(trailingComma);
                }
                else
                {
                    // Otherwise, move the trailing trivia from before to the end.
                    nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia(trailingTrivia);
                }

                var finalCollection = initialCollectionExpression.WithElements(
                    SeparatedList<CollectionElementSyntax>(nodesAndTokens));
                return finalCollection;
            }
        }

        private static CollectionElementSyntax CreateCollectionElement(
            Match<StatementSyntax>? match,
            ExpressionSyntax expression)
        {
            return match?.UseSpread is true
                ? SpreadElement(
                    Token(SyntaxKind.DotDotToken).WithLeadingTrivia(expression.GetLeadingTrivia()).WithTrailingTrivia(Space),
                    expression.WithoutLeadingTrivia())
                : ExpressionElement(expression);
        }

        private static ExpressionSyntax ConvertExpression(ExpressionSyntax expression, string? preferredIndentation)
        {
            // This must be called from an expression from the original tree.  Not something we're already transforming.
            // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
            Contract.ThrowIfNull(expression.Parent);
            return expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation, preferredIndentation),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment, preferredIndentation),
                _ => throw new InvalidOperationException(),
            };
        }

        private static AssignmentExpressionSyntax ConvertAssignment(
            AssignmentExpressionSyntax assignment,
            string? preferredIndentation)
        {
            // Assignment is only used for collection-initializers, which *currently* do not do any special
            // indentation handling on elements.
            Contract.ThrowIfTrue(preferredIndentation != null);

            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            return assignment.WithLeft(
                ImplicitElementAccess(elementAccess.ArgumentList));
        }

        private static ExpressionSyntax ConvertInvocation(
            InvocationExpressionSyntax invocation,
            string? preferredIndentation)
        {
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 1)
            {
                // Assignment expressions in a collection initializer will cause the compiler to 
                // report an error.  This is because { a = b } is the form for an object initializer,
                // and the two forms are not allowed to mix/match.  Parenthesize the assignment to
                // avoid the ambiguity.
                var expression = Indent(arguments[0].Expression, preferredIndentation);
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
            var totalLength = 0;
            if (objectCreation.Initializer != null)
            {
                foreach (var expression in objectCreation.Initializer.Expressions)
                    totalLength += expression.Span.Length;
            }

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
                }
            }

            return totalLength > wrappingLength;

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
            SeparatedSyntaxList<TNode> nodes)
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

        private static TElementSyntax CreateElement<TElementSyntax>(
            Match<StatementSyntax> match,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElementSyntax> createElement,
            string? preferredIndentation)
            where TElementSyntax : SyntaxNode
        {
            var statement = match.Statement;

            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                return createElement(match, ConvertExpression(expressionStatement.Expression, preferredIndentation));
            }
            else if (statement is ForEachStatementSyntax foreachStatement)
            {
                return createElement(match, Indent(foreachStatement.Expression, preferredIndentation));
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                var condition = Indent(ifStatement.Condition, preferredIndentation).Parenthesize();
                var trueStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Statement);

                if (ifStatement.Else is null)
                {
                    // Create: x ? [y] : []
                    var expression = ConditionalExpression(
                        condition,
                        CollectionExpression(SingletonSeparatedList<CollectionElementSyntax>(
                            ExpressionElement(ConvertExpression(trueStatement.Expression, preferredIndentation: null)))),
                        CollectionExpression());
                    return createElement(match, expression);
                }
                else
                {
                    // Create: x ? y : z
                    var falseStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                    var expression = ConditionalExpression(
                        ifStatement.Condition,
                        ConvertExpression(trueStatement.Expression, preferredIndentation: null),
                        ConvertExpression(falseStatement.Expression, preferredIndentation: null));
                    return createElement(match, expression);
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static TExpressionSyntax Indent<TExpressionSyntax>(
            SourceText text,
            IndentationOptions indentationOptions,
            TExpressionSyntax expression,
            string? preferredIndentation,
            CancellationToken cancellationToken) where TExpressionSyntax : SyntaxNode
        {
            // This must be called from an expression from the original tree.  Not something we're already transforming.
            // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
            Contract.ThrowIfNull(expression.Parent);
            if (preferredIndentation is null)
                return expression.WithoutLeadingTrivia();

            // we're starting with something either like:
            //
            //      collection.Add(some_expr +
            //          cont);
            //
            // or
            //
            //      collection.Add(
            //          some_expr +
            //              cont);
            //
            // In the first, we want to consider the `some_expr + cont` to actually start where `collection` starts so
            // that we can accurately determine where the preferred indentation should move all of it.

            var syntaxTree = expression.SyntaxTree;
            var root = syntaxTree.GetRoot(cancellationToken);
            var startLine = text.Lines.GetLineFromPosition(expression.SpanStart);

            var firstTokenOnLineIndentationString = GetIndentationStringForToken(root.FindToken(startLine.LineNumber));

            var expressionFirstToken = expression.GetFirstToken();
            return expression.ReplaceTokens(
                expression.DescendantTokens(),
                (currentToken, _) =>
                {
                    // Ensure the first token has the indentation we're moving the entire node to
                    if (currentToken == expressionFirstToken)
                        return currentToken.WithLeadingTrivia(Whitespace(preferredIndentation));

                    // If a token has any leading whitespace, it must be at the start of a line.  Whitespace is
                    // otherwise always consumed as trailing trivia if it comes after a token.
                    if (currentToken.LeadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)])
                    {
                        // First, figure out how much this token is indented *from the line* the first token was on.
                        // Then adjust the preferred indentation that amount for this token.
                        var currentTokenIndentation = GetIndentationStringForToken(currentToken);
                        var currentTokenPreferredIndentation = currentTokenIndentation.StartsWith(firstTokenOnLineIndentationString)
                            ? preferredIndentation + currentTokenIndentation[firstTokenOnLineIndentationString.Length..]
                            : preferredIndentation;
                        return currentToken.WithLeadingTrivia(Whitespace(currentTokenPreferredIndentation));
                    }

                    // Any other token is unchanged.
                    return currentToken;
                });

            string GetIndentationStringForToken(SyntaxToken token)
            {
                var tokenLine = text.Lines.GetLineFromPosition(token.SpanStart);
                var indentation = token.SpanStart - startLine.Start;
                var indentationString = new IndentationResult(indentation, offset: 0).GetIndentationString(
                    text, indentationOptions);

                return indentationString;
            }
        }

        private static SeparatedSyntaxList<ExpressionSyntax> CreateCollectionInitializerExpressions(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches)
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

                var expression = ConvertExpression(statement.Expression, preferredIndentation: null)
                    .WithTrailingTrivia().WithLeadingTrivia(leadingTrivia);

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

            return SeparatedList<ExpressionSyntax>(nodesAndTokens);
        }
    }
}
