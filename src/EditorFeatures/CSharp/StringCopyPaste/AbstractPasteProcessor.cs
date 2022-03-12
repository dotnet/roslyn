// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

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
        /// Rosly document corresponding to <see cref="SnapshotBeforePaste"/>.
        /// </summary>
        protected readonly Document DocumentBeforePaste;

        /// <summary>
        /// Rosly document corresponding to <see cref="SnapshotAfterPaste"/>.
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
            string newLine)
        {
            SnapshotBeforePaste = snapshotBeforePaste;
            SnapshotAfterPaste = snapshotAfterPaste;

            DocumentBeforePaste = documentBeforePaste;
            DocumentAfterPaste = documentAfterPaste;

            StringExpressionBeforePaste = stringExpressionBeforePaste;
            NewLine = newLine;
        }

        /// <summary>
        /// Returns true if the paste resulted in legal code for the string literal.  The string literal is
        /// considered legal if it has the same span as the original string (adjusted as per the edit) and that
        /// there are no errors in it.  For this purposes of this check, errors in interpolation holes are not
        /// considered.  We only care about the textual content of the string.
        /// </summary>
        protected bool PasteWasSuccessful(CancellationToken cancellationToken)
        {
            var rootAfterPaste = DocumentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var stringExpressionAfterPaste = FindContainingStringExpression(rootAfterPaste, StringExpressionBeforePaste.SpanStart);
            if (stringExpressionAfterPaste == null)
                return false;

            if (ContainsError(stringExpressionAfterPaste))
                return false;

            var spanAfterPaste = MapSpanForward(StringExpressionBeforePaste.Span);
            return spanAfterPaste == stringExpressionAfterPaste.Span;
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
