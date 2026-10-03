// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract partial class AbstractDocumentationCommentCommandHandler
{
    /// <param name="TrackingSpan">
    /// The selection before the paste. Edge-inclusive tracking makes it cover the text inserted by the normal editor paste.
    /// </param>
    /// <param name="Indentation">
    /// The whitespace before the comment marker, such as the four spaces in <c>    /// text</c>.
    /// This is always a prefix of <paramref name="LinePrefix"/>, and is empty for an unindented comment.
    /// It is kept separately so repeated indentation in pasted text can be removed before adding the full prefix.
    /// </param>
    /// <param name="LinePrefix">
    /// The indentation, comment marker, and whitespace following the marker. For example, <c>    /// </c> in <c>    /// text</c>.
    /// </param>
    private readonly record struct PasteContext(ITrackingSpan TrackingSpan, string Indentation, string LinePrefix);

    public void ExecutePasteCommand(PasteCommandArgs args, Action nextHandler)
    {
        if (!TryHandlePaste(args, nextHandler))
            nextHandler();
    }

    private bool TryHandlePaste(PasteCommandArgs args, Action nextHandler)
    {
        var subjectBuffer = args.SubjectBuffer;
        var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;
        var textBeforePaste = snapshotBeforePaste.AsText();
        using var _ = ArrayBuilder<PasteContext>.GetInstance(out var pasteContexts);

        foreach (var selection in args.TextView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer))
        {
            // For example, paste "A & B" into both selections in:
            //   /// <summary>[|documentation|]</summary>
            //   // [|ordinary|]
            // The editor replaces both selections. Only the first should escape '&' to '&amp;': applying XML
            // formatting to the ordinary comment would change the user's text outside documentation. Conversely,
            // skipping both would lose documentation formatting just because another selection is outside it.
            if (TryCreatePasteContext(textBeforePaste, selection, out var pasteContext))
                pasteContexts.Add(pasteContext);
        }

        if (pasteContexts.Count == 0)
            return false;

        // Let the editor perform its normal paste at every selection before inspecting the inserted text.
        // This preserves its clipboard and multi-selection behavior and keeps its paste in its own Undo step.
        nextHandler();

        AdjustPastedText(args, pasteContexts);
        return true;
    }

    private void AdjustPastedText(PasteCommandArgs args, ArrayBuilder<PasteContext> pasteContexts)
    {
        var subjectBuffer = args.SubjectBuffer;
        var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;
        using var _ = ArrayBuilder<(Span span, string replacementText)>.GetInstance(out var replacements);

        foreach (var pasteContext in pasteContexts)
        {
            // For example, after replacing [|first|] with "A & B", its tracking span covers "A & B".
            // Escaping that text to "A &amp; B" adds four characters and moves any later pasted text.
            // Compute all replacement positions before applying any changes, then use a single edit against
            // this snapshot so the positions for later selections still refer to the correct text.
            var pastedSpan = pasteContext.TrackingSpan.GetSpan(snapshotAfterPaste);
            var pastedText = pastedSpan.GetText();
            var replacementText = PreparePastedText(pastedText, pasteContext.Indentation, pasteContext.LinePrefix);

            if (pastedText != replacementText)
                replacements.Add((pastedSpan.Span, replacementText));
        }

        if (replacements.Count == 0)
            return;

        // Keep the adjustment separate from the normal editor paste. The first Undo then removes our smart
        // formatting and reveals the normal pasted text; a second Undo removes the paste itself.
        using var transaction = CaretPreservingEditTransaction.TryCreate(
            EditorFeaturesResources.Paste, args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService);

        using var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
        foreach (var (span, replacementText) in replacements)
            edit.Replace(span, replacementText);

        edit.Apply();
        transaction?.Complete();
    }

    private bool TryCreatePasteContext(SourceText text, SnapshotSpan selection, out PasteContext pasteContext)
    {
        pasteContext = default;

        var line = text.Lines.GetLineFromPosition(selection.Start.Position);
        // For a line such as "    /// text", capture "    " as indentation and "    /// " as the prefix.
        // Blank lines and lines without this language's exterior trivia are not documentation-comment contexts.
        var lineText = line.ToString();
        var exteriorTriviaOffset = lineText.GetFirstNonWhitespaceOffset() ?? -1;
        if (exteriorTriviaOffset < 0 ||
            !lineText.AsSpan(exteriorTriviaOffset).StartsWith(ExteriorTriviaText.AsSpan(), StringComparison.Ordinal))
        {
            return false;
        }

        var exteriorTriviaEnd = exteriorTriviaOffset + ExteriorTriviaText.Length;

        // Replacing indentation or the exterior trivia itself could change the comment structure. In that case,
        // leave this selection to the editor rather than applying documentation-comment formatting.
        if (selection.Start.Position < line.Start + exteriorTriviaEnd)
            return false;

        // A selection such as:
        //   /// <summary>[|first
        //   /// second|]</summary>
        // keeps the first line's marker, so pasted text still belongs to documentation. Escape it and reuse
        // that line's prefix for any pasted continuation lines, even though selected markers are replaced.
        // Check every subsequent line to avoid applying XML formatting across code, ordinary comments, or
        // blank lines. For example, preserve normal paste for "/// [|text\nclass|] C { }".
        // Include the endpoint's line even when the selection ends at its start: the selected newline
        // separates the pasted text from surviving code, so this is also a paste across that boundary.
        var lastLine = text.Lines.GetLineFromPosition(selection.End.Position);
        for (var lineNumber = line.LineNumber + 1; lineNumber <= lastLine.LineNumber; lineNumber++)
        {
            if (!LineStartsWithExteriorTrivia(text.Lines[lineNumber]))
                return false;
        }

        // Preserve the exact whitespace already used after the exterior trivia instead of synthesizing a prefix.
        var prefixEnd = exteriorTriviaEnd;
        while (prefixEnd < lineText.Length && char.IsWhiteSpace(lineText[prefixEnd]))
            prefixEnd++;

        pasteContext = new PasteContext(
            selection.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive),
            lineText[..exteriorTriviaOffset],
            lineText[..prefixEnd]);
        return true;
    }

    private static string PreparePastedText(string text, string indentation, string linePrefix)
    {
        var escapedText = DocumentationCommentSnippetHelpers.EscapePastedText(text);
        var sourceText = SourceText.From(escapedText);
        if (sourceText.Lines.Count == 1)
            return escapedText;

        using var _ = PooledStringBuilder.GetInstance(out var builder);
        builder.Append(sourceText.ToString(sourceText.Lines[0].SpanIncludingLineBreak));

        for (var i = 1; i < sourceText.Lines.Count; i++)
        {
            var line = sourceText.Lines[i];
            var lineText = line.ToString();
            var whitespaceLength = lineText.GetFirstNonWhitespaceOffset() ?? lineText.Length;
            var whitespace = lineText[..whitespaceLength];

            // Pasted text can repeat the target line's indentation after each line break. Remove that portion
            // before adding the complete documentation-comment prefix so continuation lines are not over-indented.
            if (whitespace.StartsWith(indentation, StringComparison.Ordinal))
                whitespace = whitespace[indentation.Length..];

            builder.Append(linePrefix);
            builder.Append(whitespace);
            builder.Append(lineText, whitespaceLength, lineText.Length - whitespaceLength);
            builder.Append(sourceText.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
        }

        return builder.ToString();
    }
}
