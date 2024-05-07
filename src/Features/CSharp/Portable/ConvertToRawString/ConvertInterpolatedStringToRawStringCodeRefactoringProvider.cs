// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

using static ConvertToRawStringHelpers;
using static SyntaxFactory;

internal partial class ConvertInterpolatedStringToRawStringProvider
    : AbstractConvertStringProvider<InterpolatedStringExpressionSyntax>
{
    public static readonly IConvertStringProvider Instance = new ConvertInterpolatedStringToRawStringProvider();

    private ConvertInterpolatedStringToRawStringProvider()
    {
    }

    protected override bool CheckSyntax(InterpolatedStringExpressionSyntax stringExpression)
        => stringExpression is
        {
            StringStartToken: (kind: SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedVerbatimStringStartToken),
            // TODO(cyrusn): Should we offer this on empty strings... seems undesirable as you'd end with a gigantic
            // three line alternative over just $""
            Contents.Count: > 0,
        };

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

    protected override bool CanConvert(
        ParsedDocument document,
        InterpolatedStringExpressionSyntax stringExpression,
        SyntaxFormattingOptions formattingOptions,
        out CanConvertParams convertParams,
        CancellationToken cancellationToken)
    {
        convertParams = default;

        if (stringExpression.StringStartToken.Kind() is not SyntaxKind.InterpolatedStringStartToken and not SyntaxKind.InterpolatedVerbatimStringStartToken)
            return false;

        // Check up front for syntax errors.  Knowing there are none means we don't have a sanity checks later on.
        if (stringExpression.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
            return false;

        var firstContent = stringExpression.Contents.First();
        var lastContent = stringExpression.Contents.Last();

        var priority = CodeActionPriority.Low;
        var canBeSingleLine = true;
        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolationSyntax interpolation)
            {
                if (interpolation.FormatClause != null)
                {
                    var characters = TryConvertToVirtualChars(interpolation.FormatClause.FormatStringToken);

                    // Ensure that all characters in the string are those we can convert.
                    if (!ConvertToRawStringHelpers.CanConvert(characters))
                        return false;
                }

                if (canBeSingleLine && !document.Text.AreOnSameLine(interpolation.OpenBraceToken, interpolation.CloseBraceToken))
                    canBeSingleLine = false;
            }
            else if (content is InterpolatedStringTextSyntax interpolatedStringText)
            {
                var characters = TryConvertToVirtualChars(interpolatedStringText);

                // Ensure that all characters in the string are those we can convert.
                if (!ConvertToRawStringHelpers.CanConvert(characters))
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
            stringExpression.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
        {
            var converted = GetInitialMultiLineRawInterpolatedString(stringExpression, formattingOptions);
            var cleaned = CleanInterpolatedString(converted, cancellationToken);

            canBeMultiLineWithoutLeadingWhiteSpaces = !cleaned.IsEquivalentTo(converted);
        }

        convertParams = new CanConvertParams(priority, canBeSingleLine, canBeMultiLineWithoutLeadingWhiteSpaces);
        return true;
    }

    protected override InterpolatedStringExpressionSyntax Convert(
        ParsedDocument document,
        InterpolatedStringExpressionSyntax stringExpression,
        ConvertToRawKind kind,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        if (kind == ConvertToRawKind.SingleLine)
            return ConvertToSingleLineRawString();

        var indentationOptions = new IndentationOptions(formattingOptions);

        var token = stringExpression.StringStartToken;
        var tokenLine = document.Text.Lines.GetLineFromPosition(token.SpanStart);
        if (token.SpanStart == tokenLine.Start)
        {
            // Special case.  string token starting at the start of the line.  This is a common pattern used for
            // multi-line strings that don't want any indentation and have the start/end of the string at the same
            // level (like unit tests).
            //
            // In this case, figure out what indentation we're normally like to put this string.  Update *both* the
            // contents *and* the starting quotes of the raw string.
            var indenter = document.LanguageServices.GetRequiredService<IIndentationService>();
            var indentationVal = indenter.GetIndentation(document, tokenLine.LineNumber, indentationOptions, cancellationToken);

            var indentation = indentationVal.GetIndentationString(document.Text, indentationOptions);
            var newNode = ConvertToMultiLineRawIndentedString(document, indentation);
            newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().Add(Whitespace(indentation)));
            return newNode;
        }
        else
        {
            // otherwise this was a string literal on a line that already contains contents.  Or it's a string
            // literal on its own line, but indented some amount.  Figure out the indentation of the contents from
            // this, but leave the string literal starting at whatever position it's at.
            var indentation = token.GetPreferredIndentation(document, indentationOptions, cancellationToken);
            return ConvertToMultiLineRawIndentedString(document, indentation);
        }

        InterpolatedStringExpressionSyntax ConvertToSingleLineRawString()
        {
            var (startDelimiter, endDelimiter, openBraceString, closeBraceString) = GetDelimiters(stringExpression);

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

        InterpolatedStringExpressionSyntax ConvertToMultiLineRawIndentedString(ParsedDocument document, string indentation)
        {
            var rawStringExpression = GetInitialMultiLineRawInterpolatedString(stringExpression, formattingOptions);

            // If requested, cleanup the whitespace in the expression.
            var cleanedExpression = kind == ConvertToRawKind.MultiLineWithoutLeadingWhitespace
                ? CleanInterpolatedString(rawStringExpression, cancellationToken)
                : rawStringExpression;

            var startLine = document.Text.Lines.GetLineFromPosition(GetAnchorNode(document, stringExpression).SpanStart);
            var rootAnchorIndentation = GetIndentationStringForToken(
                document.Text, formattingOptions, document.Root.FindToken(startLine.Start));

            // Now that the expression is cleaned, ensure every non-blank line gets the necessary indentation.
            var indentedText = Indent(
                cleanedExpression, formattingOptions, indentation, rootAnchorIndentation, cancellationToken);

            // Finally, parse the text back into an interpolated string so that all the contents are correct.
            var parsed = (InterpolatedStringExpressionSyntax)ParseExpression(indentedText.ToString(), options: stringExpression.SyntaxTree.Options);
            return parsed.WithTriviaFrom(stringExpression);
        }

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

        static string Indent(
            InterpolatedStringExpressionSyntax stringExpression,
            SyntaxFormattingOptions formattingOptions,
            string indentation,
            string rootAnchorIndentation,
            CancellationToken cancellationToken)
        {
            var text = stringExpression.GetText();

            using var _1 = PooledStringBuilder.GetInstance(out var builder);

            var (interpolationInteriorSpans, restrictedSpans) = GetInterpolationSpans(stringExpression, cancellationToken);

            AppendFullLine(builder, text.Lines[0]);

            for (int i = 1, n = text.Lines.Count; i < n; i++)
            {
                var line = text.Lines[i];

                if (restrictedSpans.HasIntervalThatIntersectsWith(line.Start))
                {
                    // Inside something we must not touch.  Include the line verbatim.
                    AppendFullLine(builder, line);
                    continue;
                }

                if (line.IsEmptyOrWhitespace())
                {
                    // append the original newline.
                    builder.Append(text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
                    continue;
                }

                // line with content on it.  It's either content of the string expression, or it's
                // interpolation code.
                if (interpolationInteriorSpans.Any(s => s.Contains(line.Start)))
                {
                    // inside an interpolation.  Figure out the original indentation against the appropriate anchor, and
                    // preserve that indentation on top of whatever indentation is being added.
                    var firstNonWhitespacePos = line.GetFirstNonWhitespacePosition()!.Value;

                    var anchorIndentation = DetermineAnchorIndentation(
                        rootAnchorIndentation, text, formattingOptions, stringExpression, line.Start);

                    var positionIndentation = GetIndentationStringForPosition(text, formattingOptions, firstNonWhitespacePos);
                    var preferredIndentation = positionIndentation.StartsWith(anchorIndentation)
                        ? indentation + positionIndentation[anchorIndentation.Length..]
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
    }

    private static string DetermineAnchorIndentation(
        string rootAnchorIndentation,
        SourceText text,
        SyntaxFormattingOptions formattingOptions,
        InterpolatedStringExpressionSyntax stringExpression,
        int start)
    {
        var interpolation = stringExpression.Contents.OfType<InterpolationSyntax>().Single(i => i.Span.Contains(start));
        var interpolationLine = text.Lines.GetLineFromPosition(interpolation.SpanStart);

        // We only want to do this past the first line.  The first line actually contains the contents following the
        // `"""` after we inserted the newline.  So if we have an interpolation starting there, we actually want to use
        // the indentation of string expression itself.
        if (interpolationLine.LineNumber > 1 &&
            interpolationLine.GetFirstNonWhitespacePosition() == interpolation.SpanStart)
        {
            // interpolation itself was on its own line.  We want to preserve code indentation relative to that.
            return GetIndentationStringForPosition(text, formattingOptions, interpolation.SpanStart);
        }

        // Otherwise, determine indentation based on this part of the interpolation expression relative to the string
        // literal itself.
        return rootAnchorIndentation;
    }

    private static InterpolatedStringExpressionSyntax GetInitialMultiLineRawInterpolatedString(
        InterpolatedStringExpressionSyntax stringExpression,
        SyntaxFormattingOptions formattingOptions)
    {
        // If the user asked to remove whitespace then do so now.

        // First, do the trivial conversion, just updating the start/end delimiters.  Adding the requisite newlines
        // at the start/end, and updating quotes/braces.
        var (startDelimiter, endDelimiter, openBraceString, closeBraceString) = GetDelimiters(stringExpression);

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

        return rawStringExpression;
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

    private static (TextSpanIntervalTree interpolationInteriorSpans, TextSpanIntervalTree restrictedSpans) GetInterpolationSpans(
        InterpolatedStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
    {
        var interpolationInteriorSpans = new TextSpanIntervalTree();
        var restrictedSpans = new TextSpanIntervalTree();

        SourceText? text = null;
        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolationSyntax interpolation)
            {
                interpolationInteriorSpans.AddIntervalInPlace(TextSpan.FromBounds(interpolation.OpenBraceToken.Span.End, interpolation.CloseBraceToken.Span.Start));

                // We don't want to touch any nested strings within us, mark them as off limits.  note, we only care if
                // the nested strings actually span multiple lines.  A nested string on a single line is safe to move
                // forward/back on that line without affecting runtime semantics.
                foreach (var descendant in interpolation.DescendantNodes().OfType<ExpressionSyntax>())
                {
                    if (descendant is LiteralExpressionSyntax(kind: SyntaxKind.StringLiteralExpression) ||
                        descendant is InterpolatedStringExpressionSyntax)
                    {
                        var descendantSpan = descendant.Span;

                        text ??= stringExpression.SyntaxTree.GetText(cancellationToken);
                        var startLine = text.Lines.GetLineFromPosition(descendantSpan.Start);
                        if (startLine != text.Lines.GetLineFromPosition(descendantSpan.End))
                        {
                            // If the string is the first thing on this line, then expand the restricted span to the
                            // start of the line.  We don't want to move it around at all.
                            var start = startLine.GetFirstNonWhitespacePosition() == descendantSpan.Start
                                ? startLine.Start
                                : descendantSpan.Start;
                            restrictedSpans.AddIntervalInPlace(TextSpan.FromBounds(start, descendantSpan.End));
                        }
                    }
                }
            }
        }

        return (interpolationInteriorSpans, restrictedSpans);
    }

    private static InterpolatedStringExpressionSyntax CleanInterpolatedString(
        InterpolatedStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
    {
        var text = stringExpression.GetText();

        var (interpolationInteriorSpans, restrictedSpans) = GetInterpolationSpans(stringExpression, cancellationToken);

        // Get all the lines of the string expression.  Note that the first/last lines will be the ones containing
        // the delimiters.  So they can be ignored in all further processing.
        using var _3 = ArrayBuilder<TextLine>.GetInstance(out var lines);
        lines.AddRange(text.Lines);

        // Remove the leading and trailing lines if they are all whitespace.
        while (lines[1].IsEmptyOrWhitespace() &&
            !interpolationInteriorSpans.Any(s => s.Contains(lines[1].Start)))
        {
            lines.RemoveAt(1);
        }

        while (lines[^2].IsEmptyOrWhitespace() &&
            !interpolationInteriorSpans.Any(s => s.Contains(lines[^2].Start)))
        {
            lines.RemoveAt(lines.Count - 2);
        }

        // If we removed all the lines, don't do anything.
        if (lines.Count == 2)
            return stringExpression;

        // Use the remaining lines to figure out what common whitespace we have.
        var commonWhitespacePrefix = ComputeCommonWhitespacePrefix(lines, interpolationInteriorSpans);

        using var _1 = PooledStringBuilder.GetInstance(out var builder);

        // Add the line with the starting delimiter
        AppendFullLine(builder, lines[0]);

        // Add the content lines
        for (int i = 1, n = lines.Count - 1; i < n; i++)
        {
            // ignore any blank lines we see.
            var line = lines[i];

            if (restrictedSpans.HasIntervalThatIntersectsWith(line.Start))
            {
                // Inside something we must not touch.  Include the line verbatim.
                AppendFullLine(builder, line);
                continue;
            }

            if (line.IsEmptyOrWhitespace())
            {
                // append the original newline.
                builder.Append(text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
                continue;
            }

            // line with content on it.  It's either content of the string expression, or it's
            // interpolation code.
            if (interpolationInteriorSpans.Any(s => s.Contains(line.Start)))
            {
                // Interpolation content.  Trim the prefix if present on that line, otherwise leave alone. Don't do this
                // though for restricted content as we never want to touch that.
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
    }

    private static string ComputeCommonWhitespacePrefix(
        ArrayBuilder<TextLine> lines,
        TextSpanIntervalTree interpolationInteriorSpans)
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

            if (interpolationInteriorSpans.Any(s => s.Contains(line.Start)) ||
                interpolationInteriorSpans.Any(s => s.Start - 1 == line.Start))
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

    public static SyntaxToken UpdateToken(SyntaxToken token, string text, string valueText = "", SyntaxKind? kind = null)
        => Token(
            token.LeadingTrivia,
            kind ?? token.Kind(),
            text,
            valueText == "" ? text : valueText,
            token.TrailingTrivia);
}
