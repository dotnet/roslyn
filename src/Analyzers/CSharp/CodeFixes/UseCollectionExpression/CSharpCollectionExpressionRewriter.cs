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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

internal static class CSharpCollectionExpressionRewriter
{
    /// <summary>
    /// Creates the final collection-expression <c>[...]</c> that will replace the given <paramref
    /// name="expressionToReplace"/> expression.
    /// </summary>
    public static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync<TParentExpression>(
        Document workspaceDocument,
        CodeActionOptionsProvider fallbackOptions,
        TParentExpression expressionToReplace,
        ImmutableArray<CollectionExpressionMatch> matches,
        Func<TParentExpression, InitializerExpressionSyntax?> getInitializer,
        Func<TParentExpression, InitializerExpressionSyntax, TParentExpression> withInitializer,
        CancellationToken cancellationToken)
        where TParentExpression : ExpressionSyntax
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

        var initializer = getInitializer(expressionToReplace);

        // Determine if we want to end up with a multiline collection expression.  The general intuition is that we
        // want a multiline expression if any of the following are true:
        //
        //  1. any of the elements we're going to add are multi-line themselves.
        //  2. any of the elements we're going to add will have comments on them.  These will need to be multiline
        //     so that the comments do not end up wrongly consuming other elements that come after them.
        //  3. the resultant collection expression would be very long.
        var makeMultiLineCollectionExpression = MakeMultiLineCollectionExpression();

        // If we have an initializer already with elements in it (e.g. `new List<int> { 1, 2, 3 }`) then we want to
        // preserve as much as we can from the `{ 1, 2, 3 }` portion when converting to a collection expression and
        // we want to match the style there as much as is reasonably possible.  Note that this initializer itself
        // may be multiline, and we want to match that style closely.
        //
        // If there is no existing initializer (e.g. `new List<int>();`), or the initializer has no items in it, we
        // will instead try to figure out the best form for the final collection expression based on the elements
        // we're going to add.
        return initializer == null || initializer.Expressions.Count == 0
            ? CreateCollectionExpressionWithoutExistingElements()
            : CreateCollectionExpressionWithExistingElements();

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
                var updatedRoot = document.Root.ReplaceNode(
                    expressionToReplace,
                    withInitializer(expressionToReplace, initializer));
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
                CreateAndAddElements(matches, nodesAndTokens, preferredIndentation: elementIndentation, forceTrailingComma: true);

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
                CreateAndAddElements(matches, nodesAndTokens, preferredIndentation: null, forceTrailingComma: false);

                var collectionExpression = CollectionExpression(
                    SeparatedList<CollectionElementSyntax>(nodesAndTokens));
                return collectionExpression.WithTriviaFrom(expressionToReplace);
            }
        }

        CollectionExpressionSyntax CreateCollectionExpressionWithExistingElements()
        {
            // If the object creation expression had an initializer (with at least one element in it).  Attempt to
            // preserve the formatting of the original initializer and the new collection expression.

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
                    var finalCollection = AddMatchesToExistingNonEmptyCollectionExpression(
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

                    var finalCollection = AddMatchesToExistingNonEmptyCollectionExpression(
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

                if (document.Text.AreOnSameLine(initializer.OpenBraceToken.GetPreviousToken(), initializer.OpenBraceToken))
                {
                    // Determine where both the braces and the items would like to be wrapped to.
                    var preferredBraceIndentation = initializer.OpenBraceToken.GetPreferredIndentation(document, indentationOptions, cancellationToken);
                    var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(document, indentationOptions, cancellationToken);

                    // Update both the braces and initial elements to the right location.
                    initialCollection = initialCollection.Update(
                        initialCollection.OpenBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)),
                        initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))),
                        initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredBraceIndentation)));

                    // Then add all new elements at the right indentation level.
                    var finalCollection = AddMatchesToExistingNonEmptyCollectionExpression(initialCollection, preferredItemIndentation);

                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        document.Text, initializer, finalCollection, newCollectionIsSingleLine: false);
                }
                else
                {
                    // ')' and '{' are not on the same line.  So the code looks like this:
                    //
                    //  new List<int>()
                    //  { 1, 2, 3 }

                    // Here, the brace is already in the right location.  So all we need to do is determine the preferred indentation for the items.
                    var braceIndentation = GetIndentationStringForToken(initializer.OpenBraceToken);
                    var preferredItemIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(document, indentationOptions, cancellationToken);

                    initialCollection = initialCollection
                        .WithElements(initialCollection.Elements.Replace(initialCollection.Elements.First(), initialCollection.Elements.First().WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(preferredItemIndentation))))
                        .WithCloseBracketToken(initialCollection.CloseBracketToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed, Whitespace(braceIndentation)));

                    var finalCollection = AddMatchesToExistingNonEmptyCollectionExpression(initialCollection, preferredItemIndentation);

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
                var finalCollection = AddMatchesToExistingNonEmptyCollectionExpression(initialCollection, preferredIndentation: null);

                // Now do the actual replacement.  This will ensure the location of the collection expression
                // properly corresponds to the equivalent pieces of the collection initializer.
                return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                    document.Text, initializer, finalCollection, newCollectionIsSingleLine: true);
            }
        }

        // Helper which produces the CollectionElementSyntax nodes and adds to the separated syntax list builder array.
        // Used to we can uniformly add the items correctly with the requested (but optional) indentation.  And so that
        // commas are added properly to the sequence.
        void CreateAndAddElements(
            ImmutableArray<CollectionExpressionMatch> matches,
            ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens,
            string? preferredIndentation,
            bool forceTrailingComma)
        {
            // If there's no requested indentation, then we want to produce the sequence as: `a, b, c, d`.  So just
            // a space after any comma.  If there is desired indentation for an element, then we always follow a comma
            // with a newline so that the element node comes on the next line indented properly.
            var triviaAfterComma = preferredIndentation is null
                ? TriviaList(Space)
                : TriviaList(EndOfLine(formattingOptions.NewLine));

            foreach (var element in matches.Select(m => CreateElement(m, preferredIndentation)))
            {
                AddCommaIfMissing();
                nodesAndTokens.Add(element);
            }

            if (matches.Length > 0 && forceTrailingComma)
                AddCommaIfMissing();

            return;

            void AddCommaIfMissing()
            {
                // Add a comment before each new element we're adding.  Move any trailing whitespace/comment trivia
                // from the prior node to come after that comma.  e.g. if the prior node was `x // comment` then we
                // end up with: `x, // comment<new-line>`
                if (nodesAndTokens is [.., { IsNode: true } lastNode])
                {
                    var trailingWhitespaceAndComments = lastNode.GetTrailingTrivia().Where(static t => t.IsWhitespaceOrSingleOrMultiLineComment());

                    nodesAndTokens[^1] = lastNode.WithTrailingTrivia(lastNode.GetTrailingTrivia().Where(t => !trailingWhitespaceAndComments.Contains(t)));

                    var commaToken = Token(SyntaxKind.CommaToken)
                        .WithoutLeadingTrivia()
                        .WithTrailingTrivia(TriviaList(trailingWhitespaceAndComments).AddRange(triviaAfterComma));
                    nodesAndTokens.Add(commaToken);
                }
            }
        }

        // Helper which takes a collection expression that already has at least one element in it and adds the new
        // elements to it.
        CollectionExpressionSyntax AddMatchesToExistingNonEmptyCollectionExpression(
            CollectionExpressionSyntax initialCollectionExpression,
            string? preferredIndentation)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
            nodesAndTokens.AddRange(initialCollectionExpression.Elements.GetWithSeparators());

            // If there is already a trailing comma before, remove it.  We'll add it back at the end. If there is no
            // trailing comma, then grab the trailing trivia off of the last element. We'll move it to the final
            // last element once we've added everything.
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

            // If we're wrapping to multiple lines, and we don't already have a trailing comma, then force one at the
            // end.  This keeps every element consistent with ending the line with a comma, which makes code easier to
            // maintain.
            CreateAndAddElements(
                matches, nodesAndTokens, preferredIndentation,
                forceTrailingComma: preferredIndentation != null && trailingComma == default);

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
            CollectionExpressionMatch? match, ExpressionSyntax expression)
        {
            return match?.UseSpread is true
                ? SpreadElement(
                    Token(SyntaxKind.DotDotToken).WithLeadingTrivia(expression.GetLeadingTrivia()).WithTrailingTrivia(Space),
                    expression.WithoutLeadingTrivia())
                : ExpressionElement(expression);
        }

        CollectionElementSyntax CreateElement(
            CollectionExpressionMatch match, string? preferredIndentation)
        {
            var statement = match.Statement;

            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                // Create: `x` for `collection.Add(x)` or `.. x` for `collection.AddRange(x)`
                return CreateCollectionElement(
                    match,
                    ConvertExpression(expressionStatement.Expression, expr => IndentExpression(expressionStatement, expr, preferredIndentation)));
            }
            else if (statement is ForEachStatementSyntax foreachStatement)
            {
                // Create: `.. x` for `foreach (var v in x) collection.Add(v)`
                return CreateCollectionElement(
                    match,
                    IndentExpression(foreachStatement, foreachStatement.Expression, preferredIndentation));
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                var condition = IndentExpression(ifStatement, ifStatement.Condition, preferredIndentation).Parenthesize(includeElasticTrivia: false);
                var trueStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Statement);

                if (ifStatement.Else is null)
                {
                    // Create: `x ? [y] : []` for `if (x) collection.Add(y)`
                    var expression = ConditionalExpression(
                        condition,
                        CollectionExpression(SingletonSeparatedList<CollectionElementSyntax>(
                            ExpressionElement(ConvertExpression(trueStatement.Expression, indent: null)))),
                        CollectionExpression());
                    return CreateCollectionElement(match, expression);
                }
                else
                {
                    // Create: `x ? y : z` for `if (x) collection.Add(y) else collection.Add(z)`
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

        ExpressionSyntax IndentExpression(
            StatementSyntax parentStatement,
            ExpressionSyntax expression,
            string? preferredIndentation)
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

            var startLine = document.Text.Lines.GetLineFromPosition(expression.SpanStart);

            var firstTokenOnLineIndentationString = GetIndentationStringForToken(document.Root.FindToken(startLine.Start));

            var expressionFirstToken = expression.GetFirstToken();
            var updatedExpression = expression.ReplaceTokens(
                expression.DescendantTokens(),
                (currentToken, _) =>
                {
                    // Ensure the first token has the indentation we're moving the entire node to
                    if (currentToken == expressionFirstToken)
                        return currentToken.WithLeadingTrivia(Whitespace(preferredIndentation));

                    return IndentToken(currentToken, preferredIndentation, firstTokenOnLineIndentationString);
                });

            // Now, once we've indented the expression, attempt to move comments on its containing statement to it.
            return TransferComments(parentStatement, updatedExpression, preferredIndentation);
        }

        SyntaxToken IndentToken(
            SyntaxToken token,
            string preferredIndentation,
            string firstTokenOnLineIndentationString)
        {
            // If a token has any leading whitespace, it must be at the start of a line.  Whitespace is
            // otherwise always consumed as trailing trivia if it comes after a token.
            if (token.LeadingTrivia is not [.., (kind: SyntaxKind.WhitespaceTrivia)])
                return token;

            using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var result);

            // Walk all trivia (except the final whitespace).  If we hit any comments within at the start of a line
            // indent them as well.
            for (int i = 0, n = token.LeadingTrivia.Count - 1; i < n; i++)
            {
                var currentTrivia = token.LeadingTrivia[i];
                var nextTrivia = token.LeadingTrivia[i + 1];

                var afterNewLine = i == 0 || token.LeadingTrivia[i - 1].IsEndOfLine();
                if (afterNewLine &&
                    currentTrivia.IsWhitespace() &&
                    nextTrivia.IsSingleOrMultiLineComment())
                {
                    result.Add(GetIndentedWhitespaceTrivia(
                        preferredIndentation, firstTokenOnLineIndentationString, nextTrivia.SpanStart));
                }
                else
                {
                    result.Add(currentTrivia);
                }
            }

            // Finally, figure out how much this token is indented *from the line* the first token was on.
            // Then adjust the preferred indentation that amount for this token.
            result.Add(GetIndentedWhitespaceTrivia(
                preferredIndentation, firstTokenOnLineIndentationString, token.SpanStart));

            return token.WithLeadingTrivia(TriviaList(result));
        }

        SyntaxTrivia GetIndentedWhitespaceTrivia(string preferredIndentation, string firstTokenOnLineIndentationString, int pos)
        {
            var positionIndentation = GetIndentationStringForPosition(pos);
            return Whitespace(positionIndentation.StartsWith(firstTokenOnLineIndentationString)
                ? preferredIndentation + positionIndentation[firstTokenOnLineIndentationString.Length..]
                : preferredIndentation);
        }

        static ExpressionSyntax TransferComments(
            StatementSyntax parentStatement,
            ExpressionSyntax expression,
            string preferredIndentation)
        {
            using var _1 = ArrayBuilder<SyntaxTrivia>.GetInstance(out var newLeadingTrivia);
            using var _2 = ArrayBuilder<SyntaxTrivia>.GetInstance(out var newTrailingTrivia);

            // If the statement has any leading comments, then move the range of leading trivia it has over (from
            // the first leading comment to the last).
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

                // Attempt to preserve the last newline after the last comment copied.
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
            => GetIndentationStringForPosition(token.SpanStart);

        string GetIndentationStringForPosition(int position)
        {
            var tokenLine = document.Text.Lines.GetLineFromPosition(position);
            var indentation = position - tokenLine.Start;
            var indentationString = new IndentationResult(indentation, offset: 0).GetIndentationString(
                document.Text, indentationOptions);

            return indentationString;
        }

        bool MakeMultiLineCollectionExpression()
        {
            var totalLength = 0;
            if (initializer != null)
            {
                foreach (var expression in initializer.Expressions)
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

        static ExpressionSyntax ConvertExpression(
            ExpressionSyntax expression, Func<ExpressionSyntax, ExpressionSyntax>? indent)
        {
            indent ??= static e => e;

            // This must be called from an expression from the original tree.  Not something we're already transforming.
            // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
            Contract.ThrowIfNull(expression.Parent);
            return expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation, indent),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment, indent),
                _ => throw new InvalidOperationException(),
            };
        }

        static ExpressionSyntax ConvertAssignment(
            AssignmentExpressionSyntax assignment, Func<ExpressionSyntax, ExpressionSyntax> indent)
        {
            return indent(assignment.Right);
        }

        static ExpressionSyntax ConvertInvocation(
            InvocationExpressionSyntax invocation, Func<ExpressionSyntax, ExpressionSyntax> indent)
        {
            var arguments = invocation.ArgumentList.Arguments;
            Contract.ThrowIfFalse(arguments.Count == 1);

            return indent(arguments[0].Expression);
        }
    }
}
