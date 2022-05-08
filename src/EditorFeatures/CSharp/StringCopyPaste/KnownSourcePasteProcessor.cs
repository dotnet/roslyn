// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    /// <summary>
    /// Implementation of <see cref="AbstractPasteProcessor"/> used when we know the original string literal expression
    /// we were copying text out of.  Because we know the original literal expression, we can determine what the
    /// characters being pasted meant in the original context and we can attempt to preserve that as closely as
    /// possible.
    /// </summary>
    internal class KnownSourcePasteProcessor : AbstractPasteProcessor
    {
        /// <summary>
        /// The selection in the document prior to the paste happening.
        /// </summary>
        private readonly TextSpan _selectionSpanBeforePaste;

        /// <summary>
        /// Information stored to the clipboard about the original cut/copy.
        /// </summary>
        private readonly StringCopyPasteData _copyPasteData;

        private readonly ITextBufferFactoryService2 _textBufferFactoryService;

        public KnownSourcePasteProcessor(
            string newLine,
            IndentationOptions indentationOptions,
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            TextSpan selectionSpanBeforePaste,
            StringCopyPasteData copyPasteData,
            ITextBufferFactoryService2 textBufferFactoryService)
            : base(newLine, indentationOptions, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
        {
            _selectionSpanBeforePaste = selectionSpanBeforePaste;
            _copyPasteData = copyPasteData;
            _textBufferFactoryService = textBufferFactoryService;
        }

        public override ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
            // same location the paste originally happened at.  For raw-strings things get more complex as we have to
            // deal with things like indentation and potentially adding newlines to make things legal.

            // Smart Pasting into raw string not supported yet.  
            return IsAnyRawStringExpression(StringExpressionBeforePaste)
                ? GetEditsForRawString(cancellationToken)
                : GetEditsForNonRawString();
        }

        private string EscapeForNonRawStringLiteral(string value)
            => EscapeForNonRawStringLiteral_DoNotCallDirectly(
                IsVerbatimStringExpression(StringExpressionBeforePaste),
                StringExpressionBeforePaste is InterpolatedStringExpressionSyntax,
                // We do not want to try skipping escapes in the 'value'.  We know exactly what 'value' means and don't
                // want it touched.
                trySkipExistingEscapes: false,
                value);

        private ImmutableArray<TextChange> GetEditsForNonRawString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);

            var isLiteral = StringExpressionBeforePaste is LiteralExpressionSyntax;
            foreach (var content in _copyPasteData.Contents)
            {
                if (content.IsText)
                {
                    builder.Append(EscapeForNonRawStringLiteral(content.TextValue));
                }
                else if (content.IsInterpolation)
                {
                    builder.Append('{');

                    if (isLiteral)
                    {
                        // we're copying an interpolation from an interpolated string to a string literal. For example,
                        // we're pasting `{x + y}` into the middle of `"goobar"`.  One thing we could potentially do in
                        // the future is split the literal into `"goo" + $"{x + y}" + "bar"`, or just making the
                        // containing literal into an interpolation itself.  However, for now, we do the simple thing
                        // and just treat the interpolation as raw text that should just be escaped as appropriate into
                        // the destination.
                        builder.Append(EscapeForNonRawStringLiteral(content.InterpolationExpression));
                    }
                    else
                    {
                        // we're moving an interpolation from one interpolation to another.  This can just be copied
                        // wholesale *except* for the format literal portion (e.g. `{...:XXXX}` which may have to be
                        // updated for the destination type.
                        builder.Append(content.InterpolationExpression);
                    }

                    builder.Append(content.InterpolationAlignmentClause);
                    if (content.InterpolationFormatClause != null)
                    {
                        builder.Append(':');
                        builder.Append(EscapeForNonRawStringLiteral(content.InterpolationFormatClause));
                    }

                    builder.Append('}');
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(content.Kind);
                }
            }

            return ImmutableArray.Create(new TextChange(_selectionSpanBeforePaste, builder.ToString()));
        }

        private ImmutableArray<TextChange> GetEditsForRawString(CancellationToken cancellationToken)
        {
            // To make a change to a raw string we have to go through several passes to determine what to do.
            //
            // First, just take the copied text and determine the most basic edit that would insert it into the
            // destination string.  Importantly, do not insert interpolations in this step (instead just replace them
            // with a dummy character).
            //
            // Second, after this text is inserted, look at the content regions of the string after the paste and look
            // at the sequences of `"` and `{` in them to see if we need to update the delimiters of the raw string.
            // Note: this is why it is critical that any interpolations are not inserted.  We don't want the content of
            // the interpolation to affect the delimiters.  e.g. a interpolation containing `""""` *inside* of it
            // doesn't require updating the delimiters of the outer expression.
            //
            // Also, after the text is inserted, look to see if we need to convert a single-line raw expression to
            // multi-line.
            //
            // At this point, we will now have the information necessary to actually insert the content and do things 
            // like give interpolations the proper number of braces for the final string we're making.

            DetermineTopLevelChangesToMakeToRawString(
                out var quotesToAdd, out var dollarSignsToAdd, out var convertToMultiLine);

            return DetermineTotalEditsToMakeToRawString(
                quotesToAdd, dollarSignsToAdd, convertToMultiLine, cancellationToken);
        }

        private ImmutableArray<TextChange> DetermineTotalEditsToMakeToRawString(
            string? quotesToAdd, string? dollarSignsToAdd, bool convertToMultiLine, CancellationToken cancellationToken)
        {
            var indentationWhitespace = StringExpressionBeforePaste.GetFirstToken().GetPreferredIndentation(DocumentBeforePaste, IndentationOptions, cancellationToken);

            var finalDollarSignCount = StringExpressionBeforePasteInfo.DelimiterDollarCount +
                (dollarSignsToAdd == null ? 0 : dollarSignsToAdd.Length);

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

            // First, add any extra dollar signs needed.
            if (dollarSignsToAdd != null)
                edits.Add(new TextChange(new TextSpan(StringExpressionBeforePaste.Span.Start, 0), dollarSignsToAdd));

            // Then any quotes to the start delimiter.
            if (quotesToAdd != null)
                edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.ContentSpans.First().Start, 0), quotesToAdd));

            // A newline and the indentation to start with.  Note: adding the indentation hear means that existing
            // content will start at the right location, as will any content we are pasting in.
            if (convertToMultiLine)
                edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.StartDelimiterSpan.End, 0), NewLine + indentationWhitespace));

            // If we need to add braces to existing interpolations, do so now for the interpolations before the selection.
            if (dollarSignsToAdd != null)
                UpdateExistingInterpolationBraces(edits, beforeSelection: true, dollarSignsToAdd.Length);

            // Now determine the actual content to add again, this time properly emitting it with
            // indentation/interpolations correctly.
            edits.Add(GetContentEditForRawString(insertInterpolations: true, finalDollarSignCount, indentationWhitespace));

            // If we need to add braces to existing interpolations, do so now for the interpolations before the selection.
            if (dollarSignsToAdd != null)
                UpdateExistingInterpolationBraces(edits, beforeSelection: false, dollarSignsToAdd.Length);

            // A final new-line and indentation before the end delimiter.
            if (convertToMultiLine)
                edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.EndDelimiterSpan.Start, 0), NewLine + indentationWhitespace));

            // Then  any extra quotes to the end delimiter.
            if (quotesToAdd != null)
                edits.Add(new TextChange(new TextSpan(StringExpressionBeforePasteInfo.EndDelimiterSpanWithoutSuffix.End, 0), quotesToAdd));

            return edits.ToImmutable();
        }

        private void DetermineTopLevelChangesToMakeToRawString(out string? quotesToAdd, out string? dollarSignsToAdd, out bool convertToMultiLine)
        {
            PerformInitialBasicPasteInRawString(out var textAfterBasicPaste, out var contentSpansAfterBasicPaste);

            quotesToAdd = GetQuotesToAddToRawString(textAfterBasicPaste, contentSpansAfterBasicPaste);
            dollarSignsToAdd = GetDollarSignsToAddToRawString(textAfterBasicPaste, contentSpansAfterBasicPaste);
            convertToMultiLine = !IsAnyMultiLineRawStringExpression(StringExpressionBeforePaste) && RawContentMustBeMultiLine(textAfterBasicPaste, contentSpansAfterBasicPaste);
        }

        private void PerformInitialBasicPasteInRawString(out SourceText textAfterDummyPaste, out ImmutableArray<TextSpan> contentSpansAfterDummyPaste)
        {
            var dummyContentEdit = GetContentEditForRawString(insertInterpolations: false, dollarSignCount: -1, indentationWhitespace: "");

            var clonedBuffer = _textBufferFactoryService.CreateTextBuffer(
                new SnapshotSpan(SnapshotBeforePaste, 0, SnapshotBeforePaste.Length), SnapshotBeforePaste.ContentType);
            var snapshotBeforeDummyPaste = clonedBuffer.CurrentSnapshot;

            var edit = clonedBuffer.CreateEdit();
            edit.Replace(_selectionSpanBeforePaste.ToSpan(), dummyContentEdit.NewText);

            var snapshotAfterDummyPaste = edit.Apply();
            textAfterDummyPaste = snapshotAfterDummyPaste.AsText();
            contentSpansAfterDummyPaste = StringExpressionBeforePasteInfo.ContentSpans.SelectAsArray(
                ts => MapSpan(ts, snapshotBeforeDummyPaste, snapshotAfterDummyPaste));
        }

        private void UpdateExistingInterpolationBraces(
            ArrayBuilder<TextChange> edits, bool beforeSelection, int dollarSignsToAdd)
        {
            var interpolatedStringExpression = (InterpolatedStringExpressionSyntax)StringExpressionBeforePaste;

            foreach (var content in interpolatedStringExpression.Contents)
            {
                if (content is InterpolationSyntax interpolation)
                {
                    if (beforeSelection && interpolation.Span.End > _selectionSpanBeforePaste.Start)
                        continue;

                    if (!beforeSelection && interpolation.Span.Start < _selectionSpanBeforePaste.End)
                        continue;

                    edits.Add(new TextChange(new TextSpan(interpolation.OpenBraceToken.Span.End, 0), new string('{', dollarSignsToAdd)));
                    edits.Add(new TextChange(new TextSpan(interpolation.CloseBraceToken.Span.Start, 0), new string('}', dollarSignsToAdd)));
                }
            }
        }

        private TextChange GetContentEditForRawString(
            bool insertInterpolations,
            int dollarSignCount,
            string indentationWhitespace)
        {
            dollarSignCount = Math.Max(1, dollarSignCount);
            using var _ = PooledStringBuilder.GetInstance(out var builder);

            var isLiteral = StringExpressionBeforePaste is LiteralExpressionSyntax;
            var isMultiLine = IsAnyMultiLineRawStringExpression(StringExpressionBeforePaste);

            for (var contentIndex = 0; contentIndex < _copyPasteData.Contents.Length; contentIndex++)
            {
                if (contentIndex == 0 && isMultiLine)
                {
                    TextBeforePaste.GetLineAndOffset(_selectionSpanBeforePaste.Start, out var line, out var offset);
                    if (line == TextBeforePaste.Lines.GetLineFromPosition(StringExpressionBeforePaste.SpanStart).LineNumber)
                    {
                        // the user selection starts on the line containing the leading delimiter.  e.g.
                        //
                        // var v = """ [|
                        //      content|]
                        //      """
                        //
                        // In this case, ensure we add a new-line + indentation so that the copied
                        // text will actually start in the right location.
                        builder.Append(NewLine);
                        builder.Append(indentationWhitespace);
                    }
                    else if (offset < indentationWhitespace.Length)
                    {
                        // if the line they're pasting into doesn't have enough indentation whitespace, then
                        // add enough whitespace to make the text insertion point level.  e.g.:
                        //
                        // var v = """
                        //   [|   content|]
                        //      """
                        builder.Append(indentationWhitespace[offset..]);
                    }
                }

                var content = _copyPasteData.Contents[contentIndex];
                SourceText? lastContentSourceText = null;
                if (content.IsText)
                {
                    // Convert the string to a source-text instance so we can easily process it one line at a time.
                    var sourceText = SourceText.From(content.TextValue);
                    lastContentSourceText = sourceText;

                    for (var i = 0; i < sourceText.Lines.Count; i++)
                    {
                        if (i != 0)
                            builder.Append(indentationWhitespace);

                        builder.Append(sourceText.ToString(sourceText.Lines[i].SpanIncludingLineBreak));
                    }
                }
                else if (content.IsInterpolation)
                {
                    if (!insertInterpolations)
                    {
                        // Just insert a basic string that represents the interpolation, but doesn't actually insert any
                        // potential " or { characters that might screw up later computations.
                        builder.Append('0');
                    }
                    else
                    {
                        builder.Append(new string('{', dollarSignCount));
                        builder.Append(content.InterpolationExpression);
                        builder.Append(content.InterpolationAlignmentClause);

                        if (content.InterpolationFormatClause != null)
                        {
                            builder.Append(':');
                            builder.Append(content.InterpolationFormatClause);
                        }

                        builder.Append(new string('}', dollarSignCount));
                    }
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(content.Kind);
                }

                if (contentIndex == _copyPasteData.Contents.Length - 1 && isMultiLine)
                {
                    // Similar to the check we do for the first-change, if the last change was pasted into the space
                    // before the last `"""` then we need potentially insert a newline, then enough indentation
                    // whitespace to keep delimiter in the right location.

                    TextBeforePaste.GetLineAndOffset(_selectionSpanBeforePaste.End, out var line, out var offset);

                    if (line == TextBeforePaste.Lines.GetLineFromPosition(StringExpressionBeforePaste.Span.End).LineNumber)
                    {
                        var hasNewLine = content.IsText && HasNewLine(lastContentSourceText!.Lines.Last());
                        if (!hasNewLine)
                            builder.Append(NewLine);

                        builder.Append(TextBeforePaste.ToString(new TextSpan(TextBeforePaste.Lines[line].Start, offset)));
                    }
                }
            }

            return new TextChange(_selectionSpanBeforePaste, builder.ToString());
        }
    }
}
