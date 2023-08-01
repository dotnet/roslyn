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
        /// <summary>
        /// Creates the final collection-expression <c>[...]</c> that will replace the given <paramref
        /// name="objectCreation"/> expression.
        /// </summary>
        private static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
            Document workspaceDocument,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            // This method is quite complex, but primarily because it wants to perform all the trivia handling itself.
            // We are moving nodes around in the tree in complex ways, and the formatting engine is just not sufficient
            // for performing this task.

            var document = await ParsedDocument.CreateAsync(workspaceDocument, cancellationToken).ConfigureAwait(false);

#if CODE_STYLE
            var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
#else
            var formattingOptions = await workspaceDocument.GetSyntaxFormattingOptionsAsync(
                fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

            var indentationOptions = new IndentationOptions(formattingOptions);

            // the option is currently not an editorconfig option, so not available in code style layer
#if CODE_STYLE
            var wrappingLength = CodeActionOptions.DefaultCollectionExpressionWrappingLength;
#else
            var wrappingLength = fallbackOptions.GetOptions(document.LanguageServices).CollectionExpressionWrappingLength;
#endif

            // Determine if we want to end up with a multiline collection expression.  The general intuition is that we
            // want a multiline expression if any of the following are true:
            //
            //  1. the original object creation expression was multiline.
            //  2. any of the elements we're going to add are multi-line themselves.
            //  3. any of the elements we're going to add will have comments on them.  These will need to be multiline
            //     so that the comments do not end up wrongly consuming other elements that come after them.
            //  4. any of the elements would be very long.
            var makeMultiLineCollectionExpression = MakeMultiLineCollectionExpression();

            // If we have an initializer already with elements in it (e.g. `new List<int> { 1, 2, 3 }`) then we want to
            // preserve as much as we can from the `{ 1, 2, 3 }` portion when converting to a collection expression and
            // we want to match the style there as much as is reasonably possible.  Note that this initializer itself
            // may be multiline, and we want to match that style closely.
            //
            // If there is no existing initializer (e.g. `new List<int>();`), or the initializer has no items in it, we
            // will instead try to figure out the best form for the final collection expression based on the elements
            // we're going to add.
            var initializer = objectCreation.Initializer;
            return initializer == null || initializer.Expressions.Count == 0
                ? CreateCollectionExpressionWithoutExistingElements()
                : CreateCollectionExpressionWithExistingElements();

            // Helper which produces the CollectionElementSyntax nodes and adds to the separated syntax list builder array.
            // Used to we can uniformly add the items correctly with the requested (but optional) indentation.  And so that
            // commas are added properly to the sequence.
            void CreateAndAddElements(
                ImmutableArray<Match<StatementSyntax>> matches,
                string? preferredIndentation,
                ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens)
            {
                // If there's no requested indentation, then we want to produce the sequence as: `a, b, c, d`.  So just
                // a space after any comma.  If there is desired indentation for an element, then we always follow a comma
                // with a newline so that the element node comes on the next line indented properly.
                var triviaAfterComma = preferredIndentation is null
                    ? TriviaList(Space)
                    : TriviaList(EndOfLine(formattingOptions.NewLine));

                foreach (var element in matches.Select(m => CreateElement(m, preferredIndentation)))
                {
                    // Add a comment before each new element we're adding.  Move any trailing whitespace/comment trivia
                    // from the prior node to come after that comma.  e.g. if the prior node was `x // comment` then we
                    // end up with: `x, // comment<new-line>`
                    if (nodesAndTokens.Count > 0)
                    {
                        var lastNode = nodesAndTokens[^1];
                        var trailingWhitespaceAndComments = lastNode.GetTrailingTrivia().Where(static t => t.IsWhitespaceOrSingleOrMultiLineComment());

                        nodesAndTokens[^1] = lastNode.WithTrailingTrivia(lastNode.GetTrailingTrivia().Where(t => !trailingWhitespaceAndComments.Contains(t)));

                        var commaToken = Token(SyntaxKind.CommaToken)
                            .WithoutLeadingTrivia()
                            .WithTrailingTrivia(TriviaList(trailingWhitespaceAndComments).AddRange(triviaAfterComma));
                        nodesAndTokens.Add(commaToken);
                    }

                    nodesAndTokens.Add(element);
                }
            }

            CollectionExpressionSyntax CreateCollectionExpressionWithoutExistingElements()
            {
                // Didn't have an existing initializer (or it was empty).  For both cases, just create an entirely
                // fresh collection expression, and replace the object entirely.

                if (makeMultiLineCollectionExpression)
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

                    // Update the doc with the new object (now with initializer).
                    var updatedRoot = document.Root.ReplaceNode(objectCreation, objectCreation.WithInitializer(initializer));
                    var updatedParsedDocument = document.WithChangedRoot(updatedRoot, cancellationToken);

                    // Find the '{' and 'null' tokens after the rewrite.
                    var openBraceToken = updatedRoot.GetAnnotatedTokens(openBraceTokenAnnotation).Single();
                    var nullToken = updatedRoot.GetAnnotatedTokens(nullTokenAnnotation).Single();
                    initializer = (InitializerExpressionSyntax)openBraceToken.GetRequiredParent();

                    // Figure out where those tokens would prefer to be placed if they were on their own line.
                    var openBraceIndentation = openBraceToken.GetPreferredIndentation(updatedParsedDocument, indentationOptions, cancellationToken);
                    var elementIndentation = nullToken.GetPreferredIndentation(updatedParsedDocument, indentationOptions, cancellationToken);

                    // now create the elements, following that indentation preference.
                    using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                    CreateAndAddElements(matches, preferredIndentation: elementIndentation, nodesAndTokens);

                    // Make the collection expression with the braces on new lines, at the desired brace indentation.
                    var finalCollection = CollectionExpression(
                        Token(SyntaxKind.OpenBracketToken).WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(openBraceIndentation)).WithTrailingTrivia(ElasticCarriageReturnLineFeed),
                        SeparatedList<CollectionElementSyntax>(nodesAndTokens),
                        Token(SyntaxKind.CloseBracketToken).WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(openBraceIndentation)));

                    // Now, figure out what trivia to move over from the original object over to the new collection.
                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        updatedParsedDocument.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
                }
                else
                {
                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
                    // fresh collection expression, and do a wholesale replacement of the original object creation
                    // expression with it.
                    using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                    CreateAndAddElements(matches, preferredIndentation: null, nodesAndTokens);

                    var collectionExpression = CollectionExpression(
                        SeparatedList<CollectionElementSyntax>(nodesAndTokens));
                    return collectionExpression.WithTriviaFrom(objectCreation);
                }
            }

            CollectionExpressionSyntax CreateCollectionExpressionWithExistingElements()
            {
                // If the object creation expression had an initializer (with at least one element in it).  Attempt to
                // preserve the formatting of the original initializer and the new collection expression.

                var initializer = objectCreation.Initializer;
                Contract.ThrowIfNull(initializer);

                if (!document.Text.AreOnSameLine(initializer.GetFirstToken(), initializer.GetLastToken()))
                {
                    // initializer itself was on multiple lines.  We'll want to create a collection expression whose
                    // braces (and initial elements) match whatever the initializer correct looks like.

                    var initialCollection = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (!makeMultiLineCollectionExpression &&
                        document.Text.AreOnSameLine(initializer.Expressions.First().GetFirstToken(), initializer.Expressions.Last().GetLastToken()))
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
                            document.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                    else
                    {
                        // We want the new items to be multiline *or* existing items were on different lines already.
                        // Figure out what the preferred indentation is, and prepend each new item with it.
                        var preferredIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(
                            document, indentationOptions, cancellationToken);

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            document.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                }
                else if (makeMultiLineCollectionExpression)
                {
                    // The existing initializer is on a single line.  Like: `new List<int>() { 1, 2, 3 }` But we're
                    // adding elements that want to be multi-line.  So wrap the braces, and add the new items to the
                    // end.

                    var initialCollection = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (document.Text.AreOnSameLine(objectCreation.NewKeyword, initializer.OpenBraceToken))
                    {
                        // Determine where both the braces and the items would like to be wrapped to.
                        var preferredBraceIndentation = initializer.OpenBraceToken.GetPreferredIndentation(document, indentationOptions, cancellationToken);
                        var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(document, indentationOptions, cancellationToken);

                        // Update both the braces and initial elements to the right location.
                        initialCollection = initialCollection.Update(
                            initialCollection.OpenBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)),
                            initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))),
                            initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)));

                        // Then add all new elements at the right identation level.
                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredItemIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            document.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
                    }
                    else
                    {
                        // Looks like this
                        // new List<int>()
                        // { 1, 2, 3 }

                        var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(document, indentationOptions, cancellationToken);
                        var braceIndentation = GetIndentationStringForToken(initializer.OpenBraceToken);

                        initialCollection = initialCollection
                            .WithElements(initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))))
                            .WithCloseBracketToken(initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(braceIndentation)));

                        var finalCollection = AddMatchesToExistingCollectionExpression(
                            initialCollection, preferredIndentation: preferredItemIndentation);

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            document.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
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
                        document.Text, initializer, finalCollection, newCollectionIsSingleLine: true);
                }
            }

            CollectionExpressionSyntax AddMatchesToExistingCollectionExpression(
                CollectionExpressionSyntax initialCollectionExpression,
                string? preferredIndentation)
            {
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

                CreateAndAddElements(matches, preferredIndentation, nodesAndTokens);

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
                    var condition = Indent(ifStatement, ifStatement.Condition, preferredIndentation).Parenthesize(includeElasticTrivia: false);
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
                            condition,
                            ConvertExpression(trueStatement.Expression, indent: null).Parenthesize(includeElasticTrivia: false),
                            ConvertExpression(falseStatement.Expression, indent: null).Parenthesize(includeElasticTrivia: false));
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
                var startLine = document.Text.Lines.GetLineFromPosition(expression.SpanStart);

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
                using var _1 = ArrayBuilder<SyntaxTrivia>.GetInstance(out var newLeadingTrivia);
                using var _2 = ArrayBuilder<SyntaxTrivia>.GetInstance(out var newTrailingTrivia);

                // move leading comments over.
                var leadingTrivia = parentStatement.GetLeadingTrivia();
                var firstLeadingComment = leadingTrivia.FirstOrDefault(t => t.IsSingleOrMultiLineComment());
                var lastLeadingComment = leadingTrivia.LastOrDefault(t => t.IsSingleOrMultiLineComment());
                if (firstLeadingComment != default)
                {
                    var firstLeadingCommentIndex = leadingTrivia.IndexOf(firstLeadingComment);
                    var lastLeadingCommentIndex = leadingTrivia.IndexOf(lastLeadingComment);

                    var afterNewLine = true;
                    for (var i = firstLeadingCommentIndex; i <= lastLeadingCommentIndex; i++)
                    {
                        var currentTrivia = leadingTrivia[i];
                        if (currentTrivia.IsSingleOrMultiLineComment() && afterNewLine)
                        {
                            if (newLeadingTrivia.LastOrDefault().IsWhitespace())
                                newLeadingTrivia.RemoveLast();

                            newLeadingTrivia.Add(Whitespace(preferredIndentation));
                            afterNewLine = false;
                        }

                        newLeadingTrivia.Add(currentTrivia);
                        if (currentTrivia.IsEndOfLine())
                            afterNewLine = true;
                    }

                    if (lastLeadingCommentIndex + 1 < leadingTrivia.Count &&
                        leadingTrivia[lastLeadingCommentIndex + 1].IsEndOfLine())
                    {
                        newLeadingTrivia.Add(leadingTrivia[lastLeadingCommentIndex + 1]);
                    }
                }

                // if there are trailing comments, move the trailing whitespace and comments over.
                if (parentStatement.GetTrailingTrivia().Any(static t => t.IsSingleOrMultiLineComment()))
                {
                    foreach (var trivia in parentStatement.GetTrailingTrivia())
                    {
                        if (trivia.IsWhitespaceOrSingleOrMultiLineComment())
                            newTrailingTrivia.Add(trivia);
                    }
                }

                expression = expression
                    .WithPrependedLeadingTrivia(newLeadingTrivia)
                    .WithAppendedTrailingTrivia(newTrailingTrivia);

                return expression;
            }

            string GetIndentationStringForToken(SyntaxToken token)
            {
                var tokenLine = document.Text.Lines.GetLineFromPosition(token.SpanStart);
                var indentation = token.SpanStart - tokenLine.Start;
                var indentationString = new IndentationResult(indentation, offset: 0).GetIndentationString(
                    document.Text, indentationOptions);

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
                        if (!document.Text.AreOnSameLine(component.GetFirstToken(), component.GetLastToken()))
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
