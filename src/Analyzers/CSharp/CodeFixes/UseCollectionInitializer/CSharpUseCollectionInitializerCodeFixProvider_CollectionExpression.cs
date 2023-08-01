// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    internal partial class CSharpUseCollectionInitializerCodeFixProvider
    {
        private static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
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
#if CODE_STYLE
            var wrappingLength = CodeActionOptions.DefaultCollectionExpressionWrappingLength;
#else
            var wrappingLength = fallbackOptions.GetOptions(document.Project.Services).CollectionExpressionWrappingLength;
#endif

            var makeMultiLineCollectionExpression = MakeMultiLineCollectionExpression();

            var initializer = objectCreation.Initializer;
            return initializer == null || initializer.Expressions.Count == 0
                ? CreateCollectionExpressionWithoutExistingElements()
                : CreateCollectionExpressionWithExistingElements();

            CollectionExpressionSyntax CreateCollectionExpressionWithoutExistingElements()
            {
                // Didn't have an existing initializer (or it was empty).  For both cases, just create an entirely
                // fresh collection expression, and replace the object entirely.
                if (makeMultiLineCollectionExpression)
                {
                    return SynthesizeNewMultiLineCollectionExpression(objectCreation, matches, sourceText, parsedDocument, indentationOptions, cancellationToken);
                }
                else
                {
                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
                    // fresh collection expression, and do a wholesale replacement of the original object creation
                    // expression with it.
                    var collectionExpression = CollectionExpression(
                        SeparatedList(matches.Select(m => CreateElement(m, preferredIndentation: null))));
                    return collectionExpression.WithTriviaFrom(objectCreation);
                }

                CollectionExpressionSyntax SynthesizeNewMultiLineCollectionExpression(BaseObjectCreationExpressionSyntax objectCreation, ImmutableArray<Match<StatementSyntax>> matches, SourceText? sourceText, ParsedDocument parsedDocument, IndentationOptions indentationOptions, CancellationToken cancellationToken)
                {
                    // Slightly difficult case.  We're replacing `new List<int>();` with a fresh, multi-line collection
                    // expression.  To figure out what to do here, we need to figure out where the braces *and* elements
                    // will need to go.  To figure this out, first replace `new List<int>()` with `new List<int>() { null }`
                    // then see where the indenter would place the `{` and `null` if they were on new lines.
                    var openBraceTokenAnnotation = new SyntaxAnnotation();
                    var nullTokenAnnotation = new SyntaxAnnotation();
                    var initializer = InitializerExpression(
                        SyntaxKind.CollectionInitializerExpression,
                        Token(SyntaxKind.OpenBraceToken).WithAdditionalAnnotations(openBraceTokenAnnotation),
                        SingletonSeparatedList<ExpressionSyntax>(LiteralExpression(SyntaxKind.NullLiteralExpression, Token(SyntaxKind.NullKeyword).WithAdditionalAnnotations(nullTokenAnnotation))),
                        Token(SyntaxKind.CloseBraceToken));

                    var updatedRoot = parsedDocument.Root.ReplaceNode(objectCreation, objectCreation.WithInitializer(initializer));
                    var updatedParsedDocument = parsedDocument.WithChangedRoot(updatedRoot, cancellationToken);

                    var openBraceToken = updatedRoot.GetAnnotatedTokens(openBraceTokenAnnotation).Single();
                    var nullToken = updatedRoot.GetAnnotatedTokens(nullTokenAnnotation).Single();
                    initializer = (InitializerExpressionSyntax)openBraceToken.GetRequiredParent();

                    var openBraceIndentation = openBraceToken.GetPreferredIndentation(updatedParsedDocument, indentationOptions, cancellationToken);
                    var elementIndentation = nullToken.GetPreferredIndentation(updatedParsedDocument, indentationOptions, cancellationToken);

                    var finalCollection = CollectionExpression(
                        Token(SyntaxKind.OpenBracketToken).WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(openBraceIndentation)).WithTrailingTrivia(ElasticCarriageReturnLineFeed),
                        SeparatedList(matches.Select(m => CreateElement(m, preferredIndentation: elementIndentation))),
                        Token(SyntaxKind.CloseBracketToken).WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(openBraceIndentation)));

                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        updatedParsedDocument.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
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

                    var initialCollection = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (!makeMultiLineCollectionExpression &&
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
                            initialCollection, preferredIndentation: null);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                    else
                    {
                        // We want the new items to be multiline *or* existing items were on different lines already.
                        // Figure out what the preferred indentation is, and prepend each new item with it.
                        var preferredIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(
                            parsedDocument, indentationOptions, cancellationToken);

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                }
                else if (makeMultiLineCollectionExpression)
                {
                    // The existing initializer is on a single line.  Like: `new List<int>() { 1, 2, 3 }` But we're
                    // adding elements that want to be multi-line.  So wrap the braces, and add the new items to the
                    // end.

                    var initialCollection = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (sourceText.AreOnSameLine(objectCreation.NewKeyword, initializer.OpenBraceToken))
                    {
                        var preferredBraceIndentation = initializer.OpenBraceToken.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);
                        var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

                        initialCollection = initialCollection.Update(
                            initialCollection.OpenBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)),
                            initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))),
                            initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)));

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredItemIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                    else
                    {
                        // Looks like this
                        // new List<int>()
                        // { 1, 2, 3 }

                        var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);
                        var braceIndentation = GetIndentationStringForToken(initializer.OpenBraceToken);

                        initialCollection = initialCollection
                            .WithElements(initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))))
                            .WithCloseBracketToken(initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(braceIndentation)));

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredIndentation: preferredItemIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                }
                else
                {
                    // Both the initializer and the new elements all would work on a single line.

                    // First, convert the existing initializer (and its expressions) into a corresponding collection
                    // expression.  This will fixup the braces properly for the collection expression.
                    var initialCollection = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: true);

                    // now, add all the matches in after the existing elements.
                    var finalCollection = AddMatchesToExistingCollectionExpression(
                        initialCollection, preferredIndentation: null);

                    // Now do the actual replacement.  This will ensure the location of the collection expression
                    // properly corresponds to the equivalent pieces of the collection initializer.
                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        sourceText, initializer, finalCollection, newCollectionIsSingleLine: true);
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

                foreach (var element in matches.Select(m => CreateElement(m, preferredIndentation)))
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

            static CollectionElementSyntax CreateCollectionElement(
                Match<StatementSyntax>? match, ExpressionSyntax expression)
            {
                return match?.UseSpread is true
                    ? SpreadElement(
                        Token(SyntaxKind.DotDotToken).WithLeadingTrivia(expression.GetLeadingTrivia()).WithTrailingTrivia(Space),
                        expression.WithoutLeadingTrivia())
                    : ExpressionElement(expression);
            }

            CollectionElementSyntax CreateElement(
                Match<StatementSyntax> match, string? preferredIndentation)
            {
                var statement = match.Statement;

                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    return CreateCollectionElement(
                        match,
                        ConvertExpression(expressionStatement.Expression, expr => Indent(expressionStatement, expr, preferredIndentation)));
                }
                else if (statement is ForEachStatementSyntax foreachStatement)
                {
                    return CreateCollectionElement(
                        match,
                        Indent(foreachStatement, foreachStatement.Expression, preferredIndentation));
                }
                else if (statement is IfStatementSyntax ifStatement)
                {
                    var condition = Indent(ifStatement, ifStatement.Condition, preferredIndentation).Parenthesize();
                    var trueStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Statement);

                    if (ifStatement.Else is null)
                    {
                        // Create: x ? [y] : []
                        var expression = ConditionalExpression(
                            condition,
                            CollectionExpression(SingletonSeparatedList<CollectionElementSyntax>(
                                ExpressionElement(ConvertExpression(trueStatement.Expression, indent: null)))),
                            CollectionExpression());
                        return CreateCollectionElement(match, expression);
                    }
                    else
                    {
                        // Create: x ? y : z
                        var falseStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                        var expression = ConditionalExpression(
                            ifStatement.Condition,
                            ConvertExpression(trueStatement.Expression, indent: null),
                            ConvertExpression(falseStatement.Expression, indent: null));
                        return CreateCollectionElement(match, expression);
                    }
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            TExpressionSyntax Indent<TExpressionSyntax>(
                StatementSyntax parentStatement,
                TExpressionSyntax expression,
                string? preferredIndentation) where TExpressionSyntax : SyntaxNode
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
                var startLine = sourceText.Lines.GetLineFromPosition(expression.SpanStart);

                var firstTokenOnLineIndentationString = GetIndentationStringForToken(root.FindToken(startLine.Start));

                var expressionFirstToken = expression.GetFirstToken();
                var updatedExpression = expression.ReplaceTokens(
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

                            // trim off the existing leading whitespace for this token if it has any, then add the new preferred indentation.
                            var finalLeadingTrivia = currentToken.LeadingTrivia
                                .Take(currentToken.LeadingTrivia.Count - 1)
                                .Append(Whitespace(currentTokenPreferredIndentation));

                            return currentToken.WithLeadingTrivia(finalLeadingTrivia);
                        }

                        // Any other token is unchanged.
                        return currentToken;
                    });

                return TransferComments(parentStatement, updatedExpression, preferredIndentation);
            }

            static TExpressionSyntax TransferComments<TExpressionSyntax>(
                StatementSyntax parentStatement,
                TExpressionSyntax expression,
                string preferredIndentation) where TExpressionSyntax : SyntaxNode
            {
                var statementLeadingTrivia = parentStatement.GetLeadingTrivia();
                if (!statementLeadingTrivia.Any(static t => t.IsSingleOrMultiLineComment()))
                    return expression;

                using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var finalTrivia);
                foreach (var trivia in statementLeadingTrivia)
                {
                    if (trivia.IsSingleOrMultiLineComment())
                    {
                        finalTrivia.Add(Whitespace(preferredIndentation));
                        finalTrivia.Add(trivia);
                        finalTrivia.Add(ElasticCarriageReturnLineFeed);
                    }
                }

                var finalExpression = expression.WithPrependedLeadingTrivia(finalTrivia);
                return finalExpression;
            }

            string GetIndentationStringForToken(SyntaxToken token)
            {
                var tokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);
                var indentation = token.SpanStart - tokenLine.Start;
                var indentationString = new IndentationResult(indentation, offset: 0).GetIndentationString(
                    sourceText, indentationOptions);

                return indentationString;
            }

            bool MakeMultiLineCollectionExpression()
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
            }

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

            static StatementSyntax UnwrapEmbeddedStatement(StatementSyntax statement)
                => statement is BlockSyntax { Statements: [var innerStatement] } ? innerStatement : statement;
        }
    }
}
