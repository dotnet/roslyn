// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

using static StringCopyPasteHelpers;

/// <summary>
/// Holds core before/after state related to the paste to allow subclasses to decide what text changes to make
/// without having to pass around tons of common values.
/// </summary>
internal abstract class AbstractPasteProcessor
{
    /// <summary>
    /// The buffer's snapshot prior to the paste application.
    /// </summary>
    protected readonly ITextSnapshot SnapshotBeforePaste;

    /// <summary>
    /// The buffer's snapshot right after the paste application.  Guaranteed to be exactly one version ahead of <see
    /// cref="SnapshotBeforePaste"/>.
    /// </summary>
    protected readonly ITextSnapshot SnapshotAfterPaste;

    /// <summary>
    /// Roslyn SourceText corresponding to <see cref="SnapshotBeforePaste"/>.
    /// </summary>
    protected readonly SourceText TextBeforePaste;

    /// <summary>
    /// Roslyn SourceText corresponding to <see cref="SnapshotAfterPaste"/>.
    /// </summary>
    protected readonly SourceText TextAfterPaste;

    /// <summary>
    /// Roslyn document corresponding to <see cref="SnapshotBeforePaste"/>.
    /// </summary>
    protected readonly Document DocumentBeforePaste;

    /// <summary>
    /// Roslyn document corresponding to <see cref="SnapshotAfterPaste"/>.
    /// </summary>
    protected readonly Document DocumentAfterPaste;

    /// <summary>
    /// The <see cref="LiteralExpressionSyntax"/> or <see cref="InterpolatedStringExpressionSyntax"/> that the
    /// changes were pasted into.  All changes in the paste will be in the same 'content text span' in that string
    /// expression.
    /// </summary>
    protected readonly ExpressionSyntax StringExpressionBeforePaste;

    /// <summary>
    /// Information about the relevant pieces of <see cref="StringExpressionBeforePaste"/> (like where its
    /// delimiters are).
    /// </summary>
    protected readonly StringInfo StringExpressionBeforePasteInfo;

    /// <summary>
    /// All the spans of <see cref="StringExpressionBeforePasteInfo"/>'s <see cref="StringInfo.ContentSpans"/>
    /// mapped forward (<see cref="MapSpanForward(TextSpan)"/>) to <see cref="SnapshotAfterPaste"/> in an inclusive
    /// manner. This can be used to determine what content exists post paste, and if that content requires the
    /// literal to revised to be legal.  For example, if the text content in a raw-literal contains a longer
    /// sequence of quotes after pasting, then the delimiters of the raw literal may need to be increased
    /// accordingly.
    /// </summary>
    protected readonly ImmutableArray<TextSpan> TextContentsSpansAfterPaste;

    /// <summary>
    /// User's desired new-line sequence if we need to add newlines to our text changes.
    /// </summary>
    protected readonly string NewLine;

    /// <summary>
    /// Amount to indent content in a multi-line raw string literal.
    /// </summary>
    protected readonly string IndentationWhitespace;

    /// <summary>
    /// The set of <see cref="ITextChange"/>'s that produced <see cref="SnapshotAfterPaste"/> from <see
    /// cref="SnapshotBeforePaste"/>.
    /// </summary>
    protected INormalizedTextChangeCollection Changes => SnapshotBeforePaste.Version.Changes;

    protected AbstractPasteProcessor(
        string newLine,
        string indentationWhitespace,
        ITextSnapshot snapshotBeforePaste,
        ITextSnapshot snapshotAfterPaste,
        Document documentBeforePaste,
        Document documentAfterPaste,
        ExpressionSyntax stringExpressionBeforePaste)
    {
        NewLine = newLine;
        IndentationWhitespace = indentationWhitespace;

        SnapshotBeforePaste = snapshotBeforePaste;
        SnapshotAfterPaste = snapshotAfterPaste;

        TextBeforePaste = SnapshotBeforePaste.AsText();
        TextAfterPaste = SnapshotAfterPaste.AsText();

        DocumentBeforePaste = documentBeforePaste;
        DocumentAfterPaste = documentAfterPaste;

        StringExpressionBeforePaste = stringExpressionBeforePaste;
        StringExpressionBeforePasteInfo = StringInfo.GetStringInfo(TextBeforePaste, stringExpressionBeforePaste);
        TextContentsSpansAfterPaste = StringExpressionBeforePasteInfo.ContentSpans.SelectAsArray(MapSpanForward);

        Contract.ThrowIfTrue(StringExpressionBeforePasteInfo.ContentSpans.IsEmpty);
    }

    /// <summary>
    /// Determine the edits that should be made to smartly handle pasting hte data that is on the clipboard._selectionBeforePaste
    /// </summary>
    public abstract ImmutableArray<TextChange> GetEdits();

    /// <summary>
    /// Takes a span in <see cref="SnapshotBeforePaste"/> and maps it appropriately (in an <see
    /// cref="SpanTrackingMode.EdgeInclusive"/> manner) to <see cref="SnapshotAfterPaste"/>.
    /// </summary>
    protected TextSpan MapSpanForward(TextSpan span)
        => MapSpan(span, SnapshotBeforePaste, SnapshotAfterPaste);

    /// <summary>
    /// Given an initial raw string literal, and the changes made to it by the paste, determines how many quotes to
    /// add to the start and end to keep things parsing properly.
    /// </summary>
    protected string? GetQuotesToAddToRawString(
        SourceText textAfterChange, ImmutableArray<TextSpan> textContentSpansAfterChange)
    {
        Contract.ThrowIfFalse(IsAnyRawStringExpression(StringExpressionBeforePaste));

        var longestQuoteSequence = textContentSpansAfterChange.Max(ts => GetLongestQuoteSequence(textAfterChange, ts));

        var quotesToAddCount = (longestQuoteSequence - StringExpressionBeforePasteInfo.DelimiterQuoteCount) + 1;
        return quotesToAddCount <= 0 ? null : new string('"', quotesToAddCount);
    }

    /// <summary>
    /// Given an initial raw string literal, and the changes made to it by the paste, determines how many dollar
    /// signs to add to the start to keep things parsing properly.
    /// </summary>
    protected string? GetDollarSignsToAddToRawString(
        SourceText textAfterChange, ImmutableArray<TextSpan> textContentSpansAfterChange)
    {
        Contract.ThrowIfFalse(IsAnyRawStringExpression(StringExpressionBeforePaste));

        // Only have to do this for interpolated strings.  Other strings never have a $ in their starting delimiter.
        if (StringExpressionBeforePaste is not InterpolatedStringExpressionSyntax)
            return null;

        var longestBraceSequence = textContentSpansAfterChange.Max(
            ts => Math.Max(
                GetLongestOpenBraceSequence(textAfterChange, ts),
                GetLongestCloseBraceSequence(textAfterChange, ts)));

        var dollarsToAddCount = (longestBraceSequence - StringExpressionBeforePasteInfo.DelimiterDollarCount) + 1;
        return dollarsToAddCount <= 0 ? null : new string('$', dollarsToAddCount);
    }
}
