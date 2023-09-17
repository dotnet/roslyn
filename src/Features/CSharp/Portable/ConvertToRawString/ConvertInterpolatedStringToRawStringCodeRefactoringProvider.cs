// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

using static ConvertToRawStringHelpers;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRawString), Shared]
internal partial class ConvertInterpolatedStringToRawStringCodeRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
{
    private enum ConvertToRawKind
    {
        SingleLine,
        MultiLineIndented,
        MultiLineWithoutLeadingWhitespace,
    }

    private static readonly BidirectionalMap<ConvertToRawKind, string> s_kindToEquivalenceKeyMap =
        new(new[]
        {
            KeyValuePairUtil.Create(ConvertToRawKind.SingleLine, nameof(ConvertToRawKind.SingleLine)),
            KeyValuePairUtil.Create(ConvertToRawKind.MultiLineIndented, nameof(ConvertToRawKind.MultiLineIndented)),
            KeyValuePairUtil.Create(ConvertToRawKind.MultiLineWithoutLeadingWhitespace, nameof(ConvertToRawKind.MultiLineWithoutLeadingWhitespace)),
        });

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public ConvertInterpolatedStringToRawStringCodeRefactoringProvider()
    {
    }

    protected override ImmutableArray<FixAllScope> SupportedFixAllScopes => AllFixAllScopes;

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var root = parsedDocument.Root;
        var token = root.FindToken(span.Start);
        if (!context.Span.IntersectsWith(token.Span))
            return;

        if (token.Parent is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        if (!CanConvertInterpolatedString(parsedDocument, interpolatedString, out var convertParams))
            return;

        var options = context.Options;
        var priority = convertParams.Priority;

        if (convertParams.CanBeSingleLine)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    c => UpdateDocumentAsync(document, span, ConvertToRawKind.SingleLine, options, c),
                    s_kindToEquivalenceKeyMap[ConvertToRawKind.SingleLine],
                    priority),
                token.Span);
        }
        else
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLineIndented, options, c),
                    s_kindToEquivalenceKeyMap[ConvertToRawKind.MultiLineIndented],
                    priority),
                token.Span);

            if (convertParams.CanBeMultiLineWithoutLeadingWhiteSpaces)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        CSharpFeaturesResources.without_leading_whitespace_may_change_semantics,
                        c => UpdateDocumentAsync(document, span, ConvertToRawKind.MultiLineWithoutLeadingWhitespace, options, c),
                        s_kindToEquivalenceKeyMap[ConvertToRawKind.MultiLineWithoutLeadingWhitespace],
                        priority),
                    token.Span);
            }
        }
    }

    private static VirtualCharSequence ConvertToVirtualChars(InterpolatedStringTextSyntax textSyntax)
    {
        var result = TryConvertToVirtualChars(textSyntax.TextToken);
        Contract.ThrowIfTrue(result.IsDefault);
        return result;
    }

    private static VirtualCharSequence TryConvertToVirtualChars(InterpolatedStringTextSyntax textSyntax)
        => TryConvertToVirtualChars(textSyntax.TextToken);

    private static VirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)
        => CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);

    private static bool CanConvertInterpolatedString(
        ParsedDocument document, InterpolatedStringExpressionSyntax interpolatedString, out CanConvertParams convertParams)
    {
        convertParams = default;

        if (interpolatedString.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
            return false;

        if (interpolatedString.StringStartToken.Kind() is not SyntaxKind.InterpolatedStringStartToken and not SyntaxKind.InterpolatedVerbatimStringStartToken)
            return false;

        // TODO(cyrusn): Should we offer this on empty strings... seems undesirable as you'd end with a gigantic
        // three line alternative over just $""
        if (interpolatedString.Contents.Count == 0)
            return false;

        var firstContent = interpolatedString.Contents.First();
        var lastContent = interpolatedString.Contents.Last();

        var priority = CodeActionPriority.Low;
        var canBeSingleLine = true;
        foreach (var content in interpolatedString.Contents)
        {
            if (content is InterpolationSyntax interpolation)
            {
                if (interpolation.FormatClause != null)
                {
                    var characters = TryConvertToVirtualChars(interpolation.FormatClause.FormatStringToken);

                    // Ensure that all characters in the string are those we can convert.
                    if (!CanConvert(characters))
                        return false;
                }

                if (canBeSingleLine && !document.Text.AreOnSameLine(interpolation.OpenBraceToken, interpolation.CloseBraceToken))
                    canBeSingleLine = false;
            }
            else if (content is InterpolatedStringTextSyntax interpolatedStringText)
            {
                var characters = TryConvertToVirtualChars(interpolatedStringText);

                // Ensure that all characters in the string are those we can convert.
                if (!CanConvert(characters))
                    return false;

                if (canBeSingleLine)
                {
                    // a single line raw string cannot contain a newline.
                    // Single line raw strings cannot start/end with quote.
                    if (characters.Any(static ch => IsCSharpNewLine(ch)))
                    {
                        canBeSingleLine = false;
                    }
                    else if (interpolatedStringText == firstContent &&
                        characters.First().Rune.Value == '"')
                    {
                        canBeSingleLine = false;
                    }
                    else if (interpolatedStringText == lastContent &&
                        characters.Last().Rune.Value == '"')
                    {
                        canBeSingleLine = false;
                    }
                }

                // If we have escaped quotes or braces in the string, then this is a good option to bubble up as
                // something to convert to a raw string. Otherwise, still offer this refactoring, but at low priority as
                // the user may be invoking this on lots of strings that they have no interest in converting.
                if (priority == CodeActionPriority.Low &&
                    AllEscapesAre(characters,
                        static c => c.Utf16SequenceLength == 1 && (char)c.Value is '"' or '{' or '}'))
                {
                    priority = CodeActionPriority.Default;
                }
            }
        }

        // Users sometimes write verbatim string literals with a extra starting newline (or indentation) purely
        // for aesthetic reasons.  For example:
        //
        //      var v = @"
        //          SELECT column1, column2, ...
        //          FROM table_name";
        //
        // Converting this directly to a raw string will produce:
        //
        //      var v = """
        //
        //                  SELECT column1, column2, ...
        //                  FROM table_name";
        //          """
        //
        // Check for this and offer instead to generate:
        //
        //      var v = """
        //          SELECT column1, column2, ...
        //          FROM table_name";
        //          """
        //
        // This changes the contents of the literal, but that can be fine for the domain the user is working in.
        // Offer this, but let the user know that this will change runtime semantics.
        var canBeMultiLineWithoutLeadingWhiteSpaces = false;
        if (!canBeSingleLine &&
            interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
        {
            var converted = GetInitialMultiLineRawInterpolatedString(interpolatedString);
            var cleaned = CleanInterpolatedString(converted);

            canBeMultiLineWithoutLeadingWhiteSpaces = !cleaned.IsEquivalentTo(converted);
            //if (firstContent is InterpolatedStringTextSyntax firstStringText &&
            //    HasLeadingWhitespace(ConvertToVirtualChars(firstStringText)))
            //{
            //    canBeMultiLineWithoutLeadingWhiteSpaces = true;
            //}
            //else if (lastContent is InterpolatedStringTextSyntax lastStringText &&
            //    HasTrailingWhitespace(ConvertToVirtualChars(lastStringText)))
            //{
            //    canBeMultiLineWithoutLeadingWhiteSpaces = true;
            //}

            //if (canBeMultiLineWithoutLeadingWhiteSpaces)
            //{
            //    if (Clean)
            //}
        }

        convertParams = new CanConvertParams(priority, canBeSingleLine, canBeMultiLineWithoutLeadingWhiteSpaces);
        return true;
    }

    private static async Task<Document> UpdateDocumentAsync(
        Document document, TextSpan span, ConvertToRawKind kind, CodeActionOptionsProvider optionsProvider, CancellationToken cancellationToken)
    {
        var options = await document.GetSyntaxFormattingOptionsAsync(optionsProvider, cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(span.Start);
        Contract.ThrowIfFalse(span.IntersectsWith(token.Span));

        if (token.Parent is not InterpolatedStringExpressionSyntax interpolatedString)
            return document;

        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var replacement = GetReplacementExpression(parsedDocument, interpolatedString, kind, options, cancellationToken);
        return document.WithSyntaxRoot(root.ReplaceNode(interpolatedString, replacement));
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<TextSpan> fixAllSpans,
        SyntaxEditor editor,
        CodeActionOptionsProvider optionsProvider,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        // Get the kind to be fixed from the equivalenceKey for the FixAll operation
        Debug.Assert(equivalenceKey != null);
        var kind = s_kindToEquivalenceKeyMap[equivalenceKey];

        var options = await document.GetSyntaxFormattingOptionsAsync(optionsProvider, cancellationToken).ConfigureAwait(false);
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        foreach (var fixSpan in fixAllSpans)
        {
            var node = editor.OriginalRoot.FindNode(fixSpan);
            foreach (var stringLiteral in node.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
            {
                // Ensure we can convert the string literal
                if (!CanConvertInterpolatedString(parsedDocument, stringLiteral, out var canConvertParams))
                    continue;

                // Ensure we have a matching kind to fix for this literal
                var hasMatchingKind = kind switch
                {
                    ConvertToRawKind.SingleLine => canConvertParams.CanBeSingleLine,
                    ConvertToRawKind.MultiLineIndented => !canConvertParams.CanBeSingleLine,
                    // If we started with a multi-line string that we're changing semantics for.  Then any
                    // multi-line matches are something we can proceed with.  After all, we're updating all other
                    // ones that might change semantics, so we can def update the ones that won't change semantics.
                    ConvertToRawKind.MultiLineWithoutLeadingWhitespace =>
                        !canConvertParams.CanBeSingleLine || canConvertParams.CanBeMultiLineWithoutLeadingWhiteSpaces,
                    _ => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                if (!hasMatchingKind)
                    continue;

                editor.ReplaceNode(
                    stringLiteral,
                    (current, _) =>
                    {
                        if (current is not InterpolatedStringExpressionSyntax currentStringExpression)
                            return current;

                        var currentParsedDocument = parsedDocument.WithChangedRoot(
                            current.SyntaxTree.GetRoot(cancellationToken), cancellationToken);
                        var replacement = GetReplacementExpression(
                            currentParsedDocument, currentStringExpression, kind, options, cancellationToken);
                        return replacement;
                    });
            }
        }
    }

    private static InterpolatedStringExpressionSyntax GetReplacementExpression(
        ParsedDocument parsedDocument,
        InterpolatedStringExpressionSyntax stringExpression,
        ConvertToRawKind kind,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        //var characters = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        //Contract.ThrowIfTrue(characters.IsDefaultOrEmpty);

        if (kind == ConvertToRawKind.SingleLine)
            return ConvertToSingleLineRawString();

        var indentationOptions = new IndentationOptions(formattingOptions);

        var token = stringExpression.StringStartToken;
        var tokenLine = parsedDocument.Text.Lines.GetLineFromPosition(token.SpanStart);
        if (token.SpanStart == tokenLine.Start)
        {
            // Special case.  string token starting at the start of the line.  This is a common pattern used for
            // multi-line strings that don't want any indentation and have the start/end of the string at the same
            // level (like unit tests).
            //
            // In this case, figure out what indentation we're normally like to put this string.  Update *both* the
            // contents *and* the starting quotes of the raw string.
            var indenter = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
            var indentationVal = indenter.GetIndentation(parsedDocument, tokenLine.LineNumber, indentationOptions, cancellationToken);

            var indentation = indentationVal.GetIndentationString(parsedDocument.Text, indentationOptions);
            var newNode = ConvertToMultiLineRawIndentedString(indentation);
            newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().Add(Whitespace(indentation)));
            return newNode;
        }
        else
        {
            // otherwise this was a string literal on a line that already contains contents.  Or it's a string
            // literal on its own line, but indented some amount.  Figure out the indentation of the contents from
            // this, but leave the string literal starting at whatever position it's at.
            var indentation = token.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);
            return ConvertToMultiLineRawIndentedString(indentation);
        }

        InterpolatedStringExpressionSyntax ConvertToSingleLineRawString()
        {
            var (startDelimiter, endDelimiter, openBraceString, closeBraceString) = GetDelimiters();

            return stringExpression
                .WithStringStartToken(UpdateToken(
                    stringExpression.StringStartToken,
                    startDelimiter,
                    kind: SyntaxKind.InterpolatedSingleLineRawStringStartToken))
                .WithContents(ConvertContents(stringExpression, openBraceString, closeBraceString))
                .WithStringEndToken(UpdateToken(
                    stringExpression.StringEndToken,
                    endDelimiter,
                    kind: SyntaxKind.InterpolatedRawStringEndToken));
        }

        InterpolatedStringExpressionSyntax ConvertToMultiLineRawIndentedString(string indentation)
        {
            var rawStringExpression = GetInitialMultiLineRawInterpolatedString(stringExpression);

            var cleanedExpression = rawStringExpression;
            // If requested, cleanup the whitespace in the expression.
            if (kind == ConvertToRawKind.MultiLineWithoutLeadingWhitespace)
            {
                // stringExpression = CleanupWhitespace(stringExpression);
                cleanedExpression = CleanInterpolatedString(cleanedExpression);
            }

            // Now that the expression is cleaned, ensure every non-blank line gets the necessary indentation.
            var indentedText = Indent(cleanedExpression, formattingOptions, indentation, firstTokenOnLineIndentationString);

            // Finally, parse the text back into an interpolated string so that all the contents are correct.
            var parsed = (InterpolatedStringExpressionSyntax)ParseExpression(indentedText.ToString(), options: stringExpression.SyntaxTree.Options);
            return parsed.WithTriviaFrom(stringExpression);
        }

        static string Indent(
            InterpolatedStringExpressionSyntax stringExpression,
            SyntaxFormattingOptions formattingOptions,
            string indentation,
            string firstTokenOnLineIndentationString)
        {
            var text = stringExpression.GetText();

            using var _1 = PooledStringBuilder.GetInstance(out var builder);
            using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var interpolationSpans);

            foreach (var content in stringExpression.Contents)
            {
                if (content is InterpolationSyntax interpolation)
                    interpolationSpans.Add(TextSpan.FromBounds(interpolation.OpenBraceToken.Span.End, interpolation.CloseBraceToken.Span.Start));
            }

            AppendFullLine(builder, text.Lines[0]);

            for (int i = 1, n = text.Lines.Count; i < n; i++)
            {
                var line = text.Lines[i];
                if (line.IsEmptyOrWhitespace())
                {
                    // append the original newline.
                    builder.Append(text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
                    continue;
                }

                // line with content on it.  It's either content of the string expression, or it's
                // interpolation code.

                if (interpolationSpans.Any(s => s.Contains(line.Start)))
                {
                    // inside an interpolation.  Figure out the original indentation against the appropriate anchor, and
                    // preserve that indentation on top of whatever indentation is being added.
                    var firstNonWhitespacePos = line.GetFirstNonWhitespacePosition()!.Value;

                    var positionIndentation = GetIndentationStringForPosition(text, formattingOptions, firstNonWhitespacePos);
                    var preferredIndentation = positionIndentation.StartsWith(firstTokenOnLineIndentationString)
                        ? indentation + positionIndentation[firstTokenOnLineIndentationString.Length..]
                        : indentation;
                    builder.Append(preferredIndentation);
                    builder.Append(text.ToString(TextSpan.FromBounds(firstNonWhitespacePos, line.EndIncludingLineBreak)));
                }
                else
                {
                    // Indent any content the right amount.
                    builder.Append(indentation);
                    AppendFullLine(builder, line);
                }
            }

            return builder.ToString();
        }

#if false


            // Have to make sure we have a delimiter longer than any quote sequence in the string.
            var (startDelimiter, endDelimiter, openBraceString, closeBraceString) = GetDelimiters();
            var indentationTrivia = Whitespace(indentation);

            using var _1 = ArrayBuilder<InterpolatedStringContentSyntax>.GetInstance(out var contents);

            var startToken = UpdateToken(
                stringExpression.StringStartToken,
                startDelimiter + formattingOptions.NewLine,
                kind: SyntaxKind.InterpolatedMultiLineRawStringStartToken);
            var endToken = UpdateToken(
                stringExpression.StringEndToken,
                formattingOptions.NewLine + indentation + endDelimiter,
                kind: SyntaxKind.InterpolatedRawStringEndToken);

            var first = stringExpression.Contents.First();
            var last = stringExpression.Contents.Last();

            var atStartOfLine = true;
            foreach (var content in stringExpression.Contents)
            {
                if (content is InterpolationSyntax interpolation)
                {
                    if (atStartOfLine)
                    {
                        contents.Add(InterpolatedStringText(Token(
                            leading: default,
                            SyntaxKind.InterpolatedStringTextToken,
                            indentation,
                            valueText: "",
                            trailing: default)));
                    }

                    var closeBrace = UpdateToken(interpolation.CloseBraceToken, closeBraceString);
                    if (!parsedDocument.Text.AreOnSameLine(interpolation.CloseBraceToken, interpolation.CloseBraceToken.GetPreviousToken()))
                        closeBrace = closeBrace.WithLeadingTrivia(closeBrace.LeadingTrivia.Add(indentationTrivia));

                    contents.Add(interpolation
                        .WithOpenBraceToken(UpdateToken(interpolation.OpenBraceToken, openBraceString))
                        .WithExpression(IndentExpression(interpolation.Expression, indentation))
                        .WithFormatClause(RewriteFormatClause(interpolation.FormatClause))
                        .WithCloseBraceToken(closeBrace));

                    // an interpolation never ends and then starts a new line.  Any newlines will be in a text node that follows.
                    atStartOfLine = false;
                }
                else if (content is InterpolatedStringTextSyntax stringText)
                {
                    using var _2 = PooledStringBuilder.GetInstance(out var builder);

                    var characters = ConvertToVirtualChars(stringText);
                    AppendCharacters(builder, characters, indentation, ref atStartOfLine);

                    if (atStartOfLine && stringText != stringExpression.Contents.Last())
                    {
                        builder.Append(indentation);
                        atStartOfLine = false;
                    }

                    contents.Add(stringText.WithTextToken(UpdateToken(
                        stringText.TextToken,
                        builder.ToString(),
                        characters.CreateString())));
                }
            }

            return stringExpression
                .WithStringStartToken(startToken)
                .WithContents(List(contents))
                .WithStringEndToken(endToken);

#endif
        // }

        SyntaxToken CreateToken(SyntaxKind kind, string text)
            => Token(leading: default, kind, text, text, trailing: default);

        //ExpressionSyntax IndentExpression(
        //    ExpressionSyntax expression,
        //    string preferredIndentation)
        //{
        //    var interpolation = (InterpolationSyntax)expression.GetRequiredParent();
        //    var startLine = parsedDocument.Text.Lines.GetLineFromPosition(GetAnchorNode(expression).SpanStart);
        //    var firstTokenOnLineIndentationString = GetIndentationStringForToken(parsedDocument.Root.FindToken(startLine.Start));

        //    var expressionFirstToken = expression.GetFirstToken();
        //    var updatedExpression = expression.ReplaceTokens(
        //        expression.DescendantTokens(),
        //        (currentToken, _) =>
        //        {
        //            //// Ensure the first token has the indentation we're moving the entire node to
        //            //if (currentToken == expressionFirstToken)
        //            //    return currentToken.WithLeadingTrivia(Whitespace(preferredIndentation));

        //            return IndentToken(currentToken, preferredIndentation, firstTokenOnLineIndentationString);
        //        });

        //    return updatedExpression;
        //}

        //SyntaxToken IndentToken(
        //    SyntaxToken token,
        //    string preferredIndentation,
        //    string firstTokenOnLineIndentationString)
        //{
        //    // If a token has any leading whitespace, it must be at the start of a line.  Whitespace is
        //    // otherwise always consumed as trailing trivia if it comes after a token.
        //    if (token.GetPreviousToken().TrailingTrivia is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
        //        return token;

        //    using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var result);

        //    // Walk all trivia (except the final whitespace).  If we hit any comments within at the start of a line
        //    // indent them as well.
        //    for (int i = 0, n = token.LeadingTrivia.Count - 1; i < n; i++)
        //    {
        //        var currentTrivia = token.LeadingTrivia[i];
        //        var nextTrivia = token.LeadingTrivia[i + 1];

        //        var afterNewLine = i == 0 || token.LeadingTrivia[i - 1].IsEndOfLine();
        //        if (afterNewLine &&
        //            currentTrivia.IsWhitespace() &&
        //            nextTrivia.IsSingleOrMultiLineComment())
        //        {
        //            result.Add(GetIndentedWhitespaceTrivia(
        //                preferredIndentation, firstTokenOnLineIndentationString, nextTrivia.SpanStart));
        //        }
        //        else
        //        {
        //            result.Add(currentTrivia);
        //        }
        //    }

        //    // Finally, figure out how much this token is indented *from the line* the first token was on.
        //    // Then adjust the preferred indentation that amount for this token.
        //    result.Add(GetIndentedWhitespaceTrivia(
        //        preferredIndentation, firstTokenOnLineIndentationString, token.SpanStart));

        //    return token.WithLeadingTrivia(TriviaList(result));
        //}

        //SyntaxTrivia GetIndentedWhitespaceTrivia(string preferredIndentation, string firstTokenOnLineIndentationString, int pos)
        //{
        //    var positionIndentation = GetIndentationStringForPosition(pos);
        //    return Whitespace(positionIndentation.StartsWith(firstTokenOnLineIndentationString)
        //        ? preferredIndentation + positionIndentation[firstTokenOnLineIndentationString.Length..]
        //        : preferredIndentation);
        //}
    }

    private static InterpolatedStringExpressionSyntax GetInitialMultiLineRawInterpolatedString(
        ParsedDocument document,
        InterpolatedStringExpressionSyntax stringExpression,
        SyntaxFormattingOptions formattingOptions)
    {
        // If the user asked to remove whitespace then do so now.

        // First, do the trivial conversion, just updating the start/end delimiters.  Adding the requisite newlines
        // at the start/end, and updating quotes/braces.
        var (startDelimiter, endDelimiter, openBraceString, closeBraceString) = GetDelimiters(stringExpression);

        var startLine = document.Text.Lines.GetLineFromPosition(GetAnchorNode(document, stringExpression).SpanStart);
        var firstTokenOnLineIndentationString = GetIndentationStringForToken(
            document.Text, formattingOptions, document.Root.FindToken(startLine.Start));

        // Once we have this, convert the node to text as it is much easier to process in string form.
        var rawStringExpression = stringExpression
            .WithStringStartToken(UpdateToken(
                stringExpression.StringStartToken,
                startDelimiter + formattingOptions.NewLine,
                kind: SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            .WithContents(ConvertContents(stringExpression, openBraceString, closeBraceString))
            .WithStringEndToken(UpdateToken(
                stringExpression.StringEndToken,
                formattingOptions.NewLine + endDelimiter,
                kind: SyntaxKind.InterpolatedRawStringEndToken));

        static SyntaxNode GetAnchorNode(ParsedDocument parsedDocument, SyntaxNode node)
        {
            // we're starting with something either like:
            //
            //      {some_expr +
            //          cont};
            //
            // or
            //
            //      {
            //          some_expr +
            //              cont};
            //
            // In the first, we want to consider the `some_expr + cont` to actually start where `{` starts so
            // that we can accurately determine where the preferred indentation should move all of it.
            //
            // Otherwise, default to the indentation of the line the expression is on.
            var firstToken = node.GetFirstToken();
            if (parsedDocument.Text.AreOnSameLine(firstToken.GetPreviousToken(), firstToken))
            {
                for (var current = node; current != null; current = current.Parent)
                {
                    if (current is StatementSyntax or MemberDeclarationSyntax)
                        return current;
                }
            }

            return node;
        }
    }

    private static (string startDelimiter, string endDelimiter, string openBraceString, string closeBraceString) GetDelimiters(
        InterpolatedStringExpressionSyntax stringExpression)
    {
        var (longestQuoteSequence, longestBraceSequence) = GetLongestSequences(stringExpression);

        // Have to make sure we have a delimiter longer than any quote sequence in the string.
        var quoteDelimiterCount = Math.Max(3, longestQuoteSequence + 1);
        var dollarCount = longestBraceSequence + 1;

        var quoteString = new string('"', quoteDelimiterCount);
        var startDelimiter = $"{new string('$', dollarCount)}{quoteString}";
        var openBraceString = new string('{', dollarCount);
        var closeBraceString = new string('}', dollarCount);

        return (startDelimiter, quoteString, openBraceString, closeBraceString);
    }

    private static (int longestQuoteSequence, int longestBraceSequence) GetLongestSequences(InterpolatedStringExpressionSyntax stringExpression)
    {
        var longestQuoteSequence = 0;
        var longestBraceSequence = 0;
        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolatedStringTextSyntax stringText)
            {
                var characters = ConvertToVirtualChars(stringText);
                longestQuoteSequence = Math.Max(longestQuoteSequence, GetLongestQuoteSequence(characters));
                longestBraceSequence = Math.Max(longestBraceSequence, GetLongestBraceSequence(characters));
            }
        }

        return (longestQuoteSequence, longestBraceSequence);
    }

    private static SyntaxList<InterpolatedStringContentSyntax> ConvertContents(
        InterpolatedStringExpressionSyntax stringExpression,
        string openBraceString,
        string closeBraceString)
    {
        using var _ = ArrayBuilder<InterpolatedStringContentSyntax>.GetInstance(out var contents);

        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolationSyntax interpolation)
            {
                contents.Add(interpolation
                    .WithOpenBraceToken(UpdateToken(interpolation.OpenBraceToken, openBraceString))
                    .WithFormatClause(RewriteFormatClause(interpolation.FormatClause))
                    .WithCloseBraceToken(UpdateToken(interpolation.CloseBraceToken, closeBraceString)));
            }
            else if (content is InterpolatedStringTextSyntax stringText)
            {
                var characters = ConvertToVirtualChars(stringText);
                contents.Add(stringText.WithTextToken(UpdateToken(
                    stringText.TextToken, characters.CreateString())));
            }
        }

        return List(contents);

        static InterpolationFormatClauseSyntax? RewriteFormatClause(InterpolationFormatClauseSyntax? formatClause)
        {
            if (formatClause is null)
                return null;

            var characters = TryConvertToVirtualChars(formatClause.FormatStringToken);
            return formatClause.WithFormatStringToken(UpdateToken(formatClause.FormatStringToken, characters.CreateString()));
        }
    }

    private static string GetIndentationStringForToken(SourceText text, SyntaxFormattingOptions options, SyntaxToken token)
        => GetIndentationStringForPosition(text, options, token.SpanStart);

    private static string GetIndentationStringForPosition(SourceText text, SyntaxFormattingOptions options, int position)
    {
        var lineContainingPosition = text.Lines.GetLineFromPosition(position);
        var lineText = lineContainingPosition.ToString();
        var indentation = lineText.ConvertTabToSpace(options.TabSize, initialColumn: 0, endPosition: position - lineContainingPosition.Start);
        return indentation.CreateIndentationString(options.UseTabs, options.TabSize);
    }

    private static void AppendFullLine(StringBuilder builder, TextLine line)
        => builder.Append(line.Text!.ToString(line.SpanIncludingLineBreak));

    private static InterpolatedStringExpressionSyntax CleanInterpolatedString(InterpolatedStringExpressionSyntax stringExpression)
    {
        var text = stringExpression.GetText();

        using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var interpolationSpans);

        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolationSyntax interpolation)
                interpolationSpans.Add(TextSpan.FromBounds(interpolation.OpenBraceToken.Span.End, interpolation.CloseBraceToken.Span.Start));
        }

        // Get all the lines of the string expression.  Note that the first/last lines will be the ones containing
        // the delimiters.  So they can be ignored in all further processing.
        using var _3 = ArrayBuilder<TextLine>.GetInstance(out var lines);
        lines.AddRange(text.Lines);

        // Remove the leading and trailing lines if they are all whitespace.
        while (lines[1].IsEmptyOrWhitespace() &&
            !interpolationSpans.Any(s => s.Contains(lines[1].Start)))
        {
            lines.RemoveAt(1);
        }

        while (lines[^2].IsEmptyOrWhitespace() &&
            !interpolationSpans.Any(s => s.Contains(lines[^2].Start)))
        {
            lines.RemoveAt(lines.Count - 2);
        }

        // If we removed all the lines, don't do anything.
        if (lines.Count == 2)
            return stringExpression;

        // Use the remaining lines to figure out what common whitespace we have.
        var commonWhitespacePrefix = ComputeCommonWhitespacePrefix(lines, interpolationSpans);

        using var _1 = PooledStringBuilder.GetInstance(out var builder);

        // Add the line with the starting delimiter
        AppendFullLine(builder, lines[0]);

        // Add the content lines
        for (int i = 1, n = lines.Count - 1; i < n; i++)
        {
            // ignore any blank lines we see.
            var line = lines[i];
            if (line.IsEmptyOrWhitespace())
            {
                // append the original newline.
                builder.Append(text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
                continue;
            }

            // line with content on it.  It's either content of the string expression, or it's
            // interpolation code.
            if (interpolationSpans.Any(s => s.Contains(line.Start)))
            {
                // Interpolation content.  Trim the prefix if present on that line, otherwise leave alone.
                if (line.GetFirstNonWhitespacePosition() is int pos)
                {
                    var currentLineLeadingWhitespace = line.Text!.ToString(TextSpan.FromBounds(line.Start, pos));
                    if (currentLineLeadingWhitespace.StartsWith(commonWhitespacePrefix))
                    {
                        builder.Append(text.ToString(TextSpan.FromBounds(line.Start + commonWhitespacePrefix.Length, line.EndIncludingLineBreak)));
                        continue;
                    }
                }

                AppendFullLine(builder, line);
            }
            else if (line == text.Lines[1])
            {
                // If this is the first line, then we got this line by adding a newline at the start of the
                // interpolated string, moving the contents after the quote to the next line.  In that case, the
                // next line will start at the zero-column and should not contribute to the common whitespace
                // trimming.
                AppendFullLine(builder, line);
            }
            else
            {
                // normal content. trim off of the common prefix.
                builder.Append(text.ToString(TextSpan.FromBounds(line.Start + commonWhitespacePrefix.Length, line.EndIncludingLineBreak)));
            }
        }

        // For the line before the delimiter line, trim off any trailing whitespace if present.
        var lastIndex = builder.Length;
        var beforeNewLines = lastIndex;
        while (SyntaxFacts.IsNewLine(builder[beforeNewLines - 1]))
            beforeNewLines--;

        var beforeSpaces = beforeNewLines;
        while (SyntaxFacts.IsWhitespace(builder[beforeSpaces - 1]))
            beforeSpaces--;

        builder.Remove(beforeSpaces, beforeNewLines - beforeSpaces);

        // Add the line with the final delimiter
        AppendFullLine(builder, lines[^1]);

        var parsed = (InterpolatedStringExpressionSyntax)ParseExpression(builder.ToString(), options: stringExpression.SyntaxTree.Options);
        return parsed.WithTriviaFrom(stringExpression);
        //// Remove all trailing whitespace and newlines from the final string.
        //var lastContentLine = lines[^2];
        //lastContentLine.
        //lastContentLine.GetLastNonWhitespacePosition()
        //while (lines.Count result.Count > 0 && (IsCSharpNewLine(result[^1]) || IsCSharpWhitespace(result[^1])))
        //    result.RemoveAt(result.Count - 1);
    }

    private static string CreateString(ArrayBuilder<VirtualCharSequence> lines)
    {
        using var _ = PooledStringBuilder.GetInstance(out var result);
        foreach (var line in lines)
            line.AppendTo(result);

        return result.ToString();
    }

    private static string ComputeCommonWhitespacePrefix(
        ArrayBuilder<TextLine> lines,
        ArrayBuilder<TextSpan> interpolationSpans)
    {
        string? commonLeadingWhitespace = null;
        
        // Walk all the lines between the delimiters.
        for (int i = 1, n = lines.Count - 1; i < n; i++)
        {
            if (commonLeadingWhitespace is "")
                return commonLeadingWhitespace;

            var line = lines[i];

            // If this is the first line, then we got this line by adding a newline at the start of the interpolated
            // string, moving the contents after the quote to the next line.  In that case, the next line will start at
            // the zero-column and should not contribute to the computation of the common whitespace prefix.
            if (line == line.Text!.Lines[1])
                continue;

            if (interpolationSpans.Any(s => s.Contains(line.Start)) ||
                interpolationSpans.Any(s => s.Start - 1 == line.Start))
            {
                // ignore any lines where we're inside the interpolation, or the interpolation starts at the beginning
                // of the line.
                continue;
            }

            if (line.GetFirstNonWhitespacePosition() is not int pos)
                continue;

            var currentLineLeadingWhitespace = line.Text!.ToString(TextSpan.FromBounds(line.Start, pos));
            commonLeadingWhitespace = ComputeCommonWhitespacePrefix(commonLeadingWhitespace, currentLineLeadingWhitespace);
        }

        return commonLeadingWhitespace ?? "";
    }

    private static string ComputeCommonWhitespacePrefix(
        string? leadingWhitespace1, string leadingWhitespace2)
    {
        if (leadingWhitespace1 is null)
            return leadingWhitespace2;

        var length = Math.Min(leadingWhitespace1.Length, leadingWhitespace2.Length);

        var current = 0;
        while (current < length && SyntaxFacts.IsWhitespace(leadingWhitespace1[current]) && leadingWhitespace1[current] == leadingWhitespace2[current])
            current++;

        return leadingWhitespace1[..current];
    }

    private static VirtualCharSequence GetLeadingWhitespace(VirtualCharSequence line)
    {
        var current = 0;
        while (current < line.Length && IsCSharpWhitespace(line[current]))
            current++;

        return line.GetSubSequence(TextSpan.FromBounds(0, current));
    }

    private static void BreakIntoLines(VirtualCharSequence characters, ArrayBuilder<VirtualCharSequence> lines)
    {
        var index = 0;

        while (index < characters.Length)
            lines.Add(GetNextLine(characters, ref index));
    }

    private static VirtualCharSequence GetNextLine(
        VirtualCharSequence characters,
        ref int index)
    {
        var end = index;
        while (end < characters.Length && !IsCSharpNewLine(characters[end]))
            end++;

        if (end != characters.Length)
            end += IsCarriageReturnNewLine(characters, end) ? 2 : 1;

        var result = characters.GetSubSequence(TextSpan.FromBounds(index, end));
        index = end;
        return result;
    }

    public static SyntaxToken UpdateToken(SyntaxToken token, string text, string valueText = "", SyntaxKind? kind = null)
        => Token(
            token.LeadingTrivia,
            kind ?? token.Kind(),
            text,
            valueText == "" ? text : valueText,
            token.TrailingTrivia);
}
