// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
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
        /// User's desired new-line sequence if we need to add newlines to our text changes.
        /// </summary>
        protected readonly string NewLine;

        /// <summary>
        /// Spans of text-content within <see cref="StringExpressionBeforePaste"/>.  These represent the spans where
        /// text can go within a string literal/interpolation.  Note that these spans may be empty.  For example, this
        /// happens for cases like the empty string <c>""</c>, or between interpolation holes like <c>$"x{a}{b}y"</c>.
        /// These spans can be examined to determine if pasted content is only impacting the content portion of a
        /// string, and not the delimiters or interpolation-holes.
        /// </summary>
        protected readonly ImmutableArray<TextSpan> TextContentsSpansBeforePaste;

        /// <summary>
        /// All the spans of <see cref="TextContentsSpansBeforePaste"/> mapped forward (<see
        /// cref="MapSpanForward(TextSpan)"/>) to <see cref="TextContentsSpansAfterPaste"/> in an inclusive manner. This
        /// can be used to determine what content exists post paste, and if that content requires the literal to revised
        /// to be legal.  For example, if the text content in a raw-literal contains a longer sequence of quotes after
        /// pasting, then the delimiters of the raw literal may need to be increased accordingly.
        /// </summary>
        protected readonly ImmutableArray<TextSpan> TextContentsSpansAfterPaste;

        protected readonly IndentationOptions IndentationOptions;

        /// <summary>
        /// Whether or not the string expression remained successfully parseable after the paste.  <see
        /// cref="StringCopyPasteCommandHandler.PasteWasSuccessful"/>.  If it can still be successfully parsed subclasses
        /// can adjust their view on which pieces of content need to be escaped or not.
        /// </summary>
        protected readonly bool PasteWasSuccessful;

        /// <summary>
        /// Number of quotes in the delimiter of the string being pasted into.  Given that the string should have no
        /// errors in it, this quote count should be the same for the start and end delimiter.
        /// </summary>
        protected readonly int DelimiterQuoteCount;

        /// <summary>
        /// Number of dollar signs (<c>$</c>) in the starting delimiter of the string being pasted into.
        /// </summary>
        protected readonly int DelimiterDollarCount;

        /// <summary>
        /// The set of <see cref="ITextChange"/>'s that produced <see cref="SnapshotAfterPaste"/> from <see
        /// cref="SnapshotBeforePaste"/>.
        /// </summary>
        protected INormalizedTextChangeCollection Changes => SnapshotBeforePaste.Version.Changes;

        protected AbstractPasteProcessor(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentBeforePaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            string newLine,
            IndentationOptions indentationOptions,
            bool pasteWasSuccessful)
        {
            SnapshotBeforePaste = snapshotBeforePaste;
            SnapshotAfterPaste = snapshotAfterPaste;

            TextBeforePaste = SnapshotBeforePaste.AsText();
            TextAfterPaste = SnapshotAfterPaste.AsText();

            DocumentBeforePaste = documentBeforePaste;
            DocumentAfterPaste = documentAfterPaste;

            StringExpressionBeforePaste = stringExpressionBeforePaste;
            NewLine = newLine;

            IndentationOptions = indentationOptions;
            PasteWasSuccessful = pasteWasSuccessful;

            TextContentsSpansBeforePaste = GetTextContentSpans(TextBeforePaste, stringExpressionBeforePaste, out DelimiterQuoteCount, out DelimiterDollarCount);
            TextContentsSpansAfterPaste = TextContentsSpansBeforePaste.SelectAsArray(MapSpanForward);

            Contract.ThrowIfTrue(TextContentsSpansBeforePaste.IsEmpty);
        }

        /// <summary>
        /// Takes a span in <see cref="SnapshotBeforePaste"/> and maps it appropriately (in an <see
        /// cref="SpanTrackingMode.EdgeInclusive"/> manner) to <see cref="SnapshotAfterPaste"/>.
        /// </summary>
        protected TextSpan MapSpanForward(TextSpan span)
        {
            var trackingSpan = SnapshotBeforePaste.CreateTrackingSpan(span.ToSpan(), SpanTrackingMode.EdgeInclusive);
            return trackingSpan.GetSpan(SnapshotAfterPaste).Span.ToTextSpan();
        }
    }
}
