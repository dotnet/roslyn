// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.TextDifferencing;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed partial class HtmlFormattingPass(
    IDocumentMappingService documentMappingService,
    ILoggerFactory loggerFactory) : IFormattingPass
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<HtmlFormattingPass>();

    public async Task<ImmutableArray<TextChange>> ExecuteAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        var changedText = context.SourceText;

        if (changes.Length > 0)
        {
            context.Logger?.LogSourceText("HtmlSourceText", context.CodeDocument.GetHtmlSourceText(cancellationToken));

            // There is a lot of uncertainty when we're dealing with edits that come from the Html formatter
            // because we are not responsible for it. It could make all sorts of strange edits, and it could
            // structure those edits is all sorts of ways. eg, it could have individual character edits, or
            // it could have a single edit that replaces a whole section of text, or the whole document.
            // Since the Html formatter doesn't understand Razor, and in fact doesn't even format the actual
            // Razor document directly (all C# is replaced), we have to be selective about what edits we will
            // actually use, but being selective is tricky because we might be missing some intentional edits
            // that the formatter made.
            //
            // To solve this, and work around various issues due to the Html formatter seeing a much simpler
            // document that we are actually dealing with, the first thing we do is take the changes it suggests
            // and apply them to the document it saw, then use our own algorithm to produce a set of changes
            // that more closely match what we want to get out of it. Specifically, we only want to see changes
            // to whitespace, or Html, not changes that include C#. Fortunately since we encode all C# as tildes
            // it means we can do a word-based diff, and all C# will essentially be equal to all other C#, so
            // won't appear in the diff.
            //
            // So we end up with a set of changes that are only ever to whitespace, or legitimate Html (though
            // in reality the formatter doesn't change that anyway).

            // Avoid computing a minimal diff if we don't need to. Slightly wasteful if we've come from one
            // of the other overloads, but worth it if we haven't (and worth it for them to validate before
            // doing the work to convert edits to changes).
            if (changes.Any(static e => e.NewText?.Contains('~') ?? false))
            {
                var htmlSourceText = context.CodeDocument.GetHtmlSourceText(cancellationToken);
                var htmlWithChanges = htmlSourceText.WithChanges(changes);

                changes = SourceTextDiffer.GetMinimalTextChanges(htmlSourceText, htmlWithChanges, DiffKind.Word);
                if (changes.Length == 0)
                {
                    return [];
                }
            }

            // Apply the line-by-line filtering algorithm
            var filteredChanges = await FilterIncomingChangesAsync(context, changes, cancellationToken).ConfigureAwait(false);
            if (filteredChanges.Length == 0)
            {
                return [];
            }

            changedText = changedText.WithChanges(filteredChanges);

            context.Logger?.LogSourceText("AfterHtmlFormatter", changedText);
        }

        return SourceTextDiffer.GetMinimalTextChanges(context.SourceText, changedText, DiffKind.Char);
    }

    private async Task<ImmutableArray<TextChange>> FilterIncomingChangesAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        var codeDocument = context.CodeDocument;
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        var originalText = codeDocument.Source.Text;

        var csharpSyntaxTree = await context.OriginalSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var csharpSyntaxRoot = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        // Apply all changes to create the formatted document
        var formattedText = originalText.WithChanges(changes);
        context.Logger?.LogSourceText("UnfilteredFormattedHtmlSourceText", formattedText);

        // Filter out any change that happens in a C# literal. We can't do this based on text easily, as there could
        // be any number of C# literals on a line, so we have to do it at the edit level, after computing character
        // level edits.
        //
        // eg, <div><span>@("hello   there")</<span><span>@("hello   there")</<span></div>
        //
        // The Html formatter would remove the whitespace in the string literal, but if we go line-by-line
        // we'd have to work very hard to detect that, and if we just process edits we could miss it because
        // there could be one edit to replace the whole line.
        changes = SourceTextDiffer.GetMinimalTextChanges(originalText, formattedText, DiffKind.Char);
        changes = FilterChangesInStringLiterals(changes);

        // Re-apply the changes to get the new formatted text
        formattedText = originalText.WithChanges(changes);

        context.Logger?.LogSourceText("FormattedHtmlSourceText", formattedText);

        // Compute the line metadata, to tell the formatting helper how to deal with each line
        var lineInfo = GenerateLineInfo(codeDocument, originalText);

        context.Logger?.LogObject("HtmlFormattingLineInfo", lineInfo);

        // Now go line-by-line and build the final changes by selecting what to keep from each line
        using var formattingChanges = new PooledArrayBuilder<TextChange>();
        FormattingUtilities.GetOriginalDocumentChangesFromLineInfo(context, originalText, lineInfo, formattedText, _logger, ShouldKeepInsertedNewLine, ref formattingChanges.AsRef(), out _);

        var finalFormattingChanges = formattingChanges.ToArray();
        context.Logger?.LogObject("FinalHtmlFormattingChanges", finalFormattingChanges);
        var changedText = originalText.WithChanges(finalFormattingChanges);
        context.Logger?.LogSourceText("FinalHtmlFormattedDocument", changedText);

        // Finally, one more pass to compute the minial changes as the algorithm we use above is pretty naive
        // and will have lots of changes that don't actually change anything.
        return SourceTextDiffer.GetMinimalTextChanges(context.SourceText, changedText, DiffKind.Char);

        bool ShouldKeepInsertedNewLine(int originalPosition)
        {
            Debug.Assert(originalPosition < originalText.Length);

            // When render fragments are inside a C# code block, eg:
            //
            // @code {
            //      void Foo()
            //      {
            //          Render(@<SurveyPrompt />);
            //      }
            // }
            //
            // This is popular in some libraries, like bUnit. The issue here is that
            // the Html formatter sees ~~~~~<SurveyPrompt /> and puts a newline before
            // the tag, but obviously that breaks things by separating the transition and the tag.
            if (originalPosition > 0 &&
                originalText[originalPosition - 1] == '@' &&
                originalText[originalPosition] == '<')
            {
                return false;
            }

            // String literal protection - check if newline was added in a string literal.
            // We check at the position of the newline, which is the end of the formatted line, but need
            // to translate that back to the original document position.
            // There is a good chance this is unnecessary, based on the pre-filtering we did above but
            // since we're at the point where we know for sure a newline was added, and there shouldn't
            // be too many of those scenarios, its worth being extra safe, because the pre-filtering is
            // at the mercy of the exact shape of the edits the Html formatter made.
            if (IsInStringLiteral(originalPosition))
            {
                return false;
            }

            return true;
        }

        ImmutableArray<TextChange> FilterChangesInStringLiterals(ImmutableArray<TextChange> changes)
        {
            using var validChanges = new PooledArrayBuilder<TextChange>();
            foreach (var change in changes)
            {
                if (IsInStringLiteral(change.Span.Start))
                {
                    continue;
                }

                validChanges.Add(change);
            }

            if (changes.Length == validChanges.Count)
            {
                return changes;
            }

            return validChanges.ToImmutableAndClear();
        }

        bool IsInStringLiteral(int position)
        {
            if (_documentMappingService.TryMapToCSharpDocumentPosition(csharpDocument, position, out _, out var csharpIndex) &&
                csharpSyntaxRoot.FindNode(new TextSpan(csharpIndex, 0), getInnermostNodeForTie: true) is { } csharpNode &&
                csharpNode.IsStringLiteral())
            {
                return true;
            }

            return false;
        }
    }

    private static ImmutableArray<LineInfo> GenerateLineInfo(RazorCodeDocument codeDocument, SourceText originalText)
    {
        var (scriptAndStyleSpans, razorCommentSpans) = BuildSpans(codeDocument, originalText);

        using var lineInfoBuilder = new PooledArrayBuilder<LineInfo>(capacity: originalText.Lines.Count);

        // Build LineInfo for each line in the original document.
        // We try to find the corresponding line in the formatted document by matching
        // non-whitespace content. This handles cases where lines are shifted.
        foreach (var originalLine in originalText.Lines)
        {
            var lineStart = originalLine.Start;

            // Determine processing flags based on context
            // For most lines, we don't change indentation, but allow formatting
            var processIndentation = false;
            var processFormatting = true;

            // A line can start inside a multiline Razor comment and still have real markup after the comment closes.
            // Only suppress processing when the rest of the line is whitespace, so trailing Html can still be split out.
            if (TryGetContainingSpan(lineStart, razorCommentSpans, out var razorCommentSpan) &&
                HasOnlyWhitespaceAfterSpan(originalText, originalLine, razorCommentSpan))
            {
                // Inside Razor comments: don't process anything
                processIndentation = false;
                processFormatting = false;
            }
            else if (TryGetContainingSpan(lineStart, scriptAndStyleSpans, out _))
            {
                // Inside script/style tags: process both indentation and formatting
                processIndentation = true;
                processFormatting = true;
            }

            lineInfoBuilder.Add(new LineInfo(
                ProcessIndentation: processIndentation,
                ProcessFormatting: processFormatting,
                CheckForNewLines: true,
                // Everything below here is default/unused for Html formatting
                SkippedPreviousLineOriginOffset: null,
                SkipNextLine: false,
                SkipNextLineIfBrace: false,
                FixedIndentLevel: 0,
                OriginOffset: 0,
                FormattedLength: 0,
                FormattedOffset: 0,
                FormattedOffsetFromEndOfLine: 0,
                AdditionalIndentation: null));
        }

        return lineInfoBuilder.ToImmutable();
    }

    /// <summary>
    /// Builds arrays of TextSpans for script/style elements and Razor comments in a single tree traversal.
    /// </summary>
    private static (ImmutableArray<TextSpan> ScriptAndStyleSpans, ImmutableArray<TextSpan> RazorCommentSpans) BuildSpans(
        RazorCodeDocument codeDocument,
        SourceText sourceText)
    {
        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();

        using var scriptStyleBuilder = new PooledArrayBuilder<TextSpan>();
        using var commentBuilder = new PooledArrayBuilder<TextSpan>();

        foreach (var node in syntaxRoot.DescendantNodes())
        {
            if (node is BaseMarkupElementSyntax element &&
                element.StartTag is { } startTag &&
                element.EndTag is { } endTag &&
                RazorSyntaxFacts.IsScriptOrStyleBlock(element) &&
                element.GetLinePositionSpan(codeDocument.Source).SpansMultipleLines())
            {
                // We only want the contents of the script tag to be included, but not whitespace before the end tag if
                // there is only whitespace before the tag, so the calculation of the end is a little annoying.
                // eg, if the last line is just "    </script>", then the contents end at the start of the line, so
                // we are free to modify the whitespace in front of the end tag. If the last line is "   foo();</script>"
                // however, then we want the Html formatter to be in charge of the whitespace, so the contents end at the "f";
                var endTagLine = sourceText.Lines.GetLineFromPosition(endTag.SpanStart);
                var firstNonWhitespace = endTagLine.GetFirstNonWhitespacePosition();
                var end = firstNonWhitespace == endTag.SpanStart
                    ? endTagLine.Start
                    : firstNonWhitespace.GetValueOrDefault() + 1;
                scriptStyleBuilder.Add(TextSpan.FromBounds(startTag.EndPosition, end));
            }
            else if (node is RazorCommentBlockSyntax comment &&
                comment.GetLinePositionSpan(codeDocument.Source).SpansMultipleLines())
            {
                // Razor comment
                commentBuilder.Add(comment.Span);
            }
        }

        return (scriptStyleBuilder.ToImmutable(), commentBuilder.ToImmutable());
    }

    private static bool HasOnlyWhitespaceAfterSpan(SourceText originalText, TextLine line, TextSpan span)
    {
        var endLine = originalText.Lines.GetLineFromPosition(span.End);
        if (endLine != line)
        {
            return false;
        }

        return line.GetFirstNonWhitespaceOffset(startOffset: span.End - line.Start) is null;
    }

    private static bool TryGetContainingSpan(int position, ImmutableArray<TextSpan> spans, out TextSpan span)
    {
        if (spans.Length == 0)
        {
            span = default;
            return false;
        }

        var index = spans.BinarySearchBy(position, static (span, pos) =>
        {
            if (span.Contains(pos))
            {
                return 0;
            }

            return span.Start.CompareTo(pos);
        });

        if (index < 0)
        {
            span = default;
            return false;
        }

        span = spans[index];
        return true;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(HtmlFormattingPass pass)
    {
        public Task<ImmutableArray<TextChange>> FilterIncomingChangesAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
            => pass.FilterIncomingChangesAsync(context, changes, cancellationToken);
    }
}
