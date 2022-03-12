// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    [Export(typeof(ICommandHandler))]
    [VSUtilities.ContentType(ContentTypeNames.CSharpContentType)]
    [VSUtilities.Name(nameof(StringCopyPasteCommandHandler))]
    internal partial class StringCopyPasteCommandHandler : IChainedCommandHandler<CopyCommandArgs>, IChainedCommandHandler<PasteCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IGlobalOptionService _globalOptions;

        private NormalizedSnapshotSpanCollection? _lastSelectedSpans;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StringCopyPasteCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IGlobalOptionService globalOptions)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _globalOptions = globalOptions;
        }

        public string DisplayName => nameof(StringCopyPasteCommandHandler);

        #region Copy

        public CommandState GetCommandState(CopyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(CopyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Ensure that the copy always goes through all other handlers.
            nextCommandHandler();

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            _lastSelectedSpans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        #endregion

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (!_globalOptions.GetOption(FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste))
                return;

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            var selectionsBeforePaste = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // if we're not even sure where the user caret/selection is on this buffer, we can't proceed.
            if (selectionsBeforePaste.Count == 0)
                return;

            var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;

            // Always let the real paste go through.  That way we always have a version of the document that doesn't
            // include our changes that we can undo back to.
            nextCommandHandler();

            var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;

            // If there were multiple changes that already happened, then don't make any changes.  Some other component
            // already did something advanced.
            if (snapshotAfterPaste.Version != snapshotBeforePaste.Version.Next)
                return;

            // If the user pasted something other than the last piece of text we're tracking, then that means some other
            // copy happened, and we can't do anything special here.
            //if (PastedTextEqualsLastCopiedText(subjectBuffer))
            //{
            //    // ProcessPasteFromKnownSource();
            //}
            //else
            //{

            // Have to even be in a C# doc to be able to do anything here.

            var documentBeforePaste = snapshotBeforePaste.GetOpenDocumentInCurrentContextWithChanges();
            var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
            if (documentBeforePaste == null || documentAfterPaste == null)
                return;

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

            var rootBeforePaste = documentBeforePaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

            // When pasting, only do anything special if the user selections were entirely inside a single string
            // literal token.  Otherwise, we have a multi-selection across token kinds which will be extremely 
            // complex to try to reconcile.
            if (!AllChangesInSameStringToken(rootBeforePaste, snapshotBeforePaste.AsText(), selectionsBeforePaste, out var stringExpressionBeforePaste))
                return;

            var processor = new UnknownSourcePasteProcessor(
                textView,
                subjectBuffer,
                snapshotBeforePaste,
                snapshotAfterPaste,
                documentBeforePaste,
                documentAfterPaste,
                stringExpressionBeforePaste);

            var textChanges = processor.GetTextChanges(cancellationToken);
            var newTextAfterChanges = snapshotBeforePaste.AsText().WithChanges(textChanges);

            // If we end up making the same changes as what the paste did, then no need to proceed.
            if (ContentsAreSame(snapshotBeforePaste, snapshotAfterPaste, stringExpressionBeforePaste, newTextAfterChanges))
                return;

            var newDocument = documentAfterPaste.WithText(newTextAfterChanges);

            using var transaction = new CaretPreservingEditTransaction(
                CSharpEditorResources.Fixing_string_literal_after_paste,
                textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
            transaction.Complete();
        }

        /// <summary>
        /// Given the snapshots before/after pasting, and the source-text our manual fixup edits produced, see if our
        /// manual application actually produced the same results as the paste.  If so, we don't need to actually do
        /// anything.  To optimize this check, we pass in the original string expression as that's all we have to check
        /// (adjusting for where it now ends up) in both the 'after' documents.
        /// </summary>
        private static bool ContentsAreSame(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            SourceText newTextAfterChanges)
        {
            // We ended up with documents of different length after we escapes/manipulated the pasted text.  So the 
            // contents are definitely not the same.
            if (newTextAfterChanges.Length != snapshotAfterPaste.Length)
                return false;

            var trackingSpan = snapshotBeforePaste.CreateTrackingSpan(stringExpressionBeforePaste.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
            var spanAfterPaste = trackingSpan.GetSpan(snapshotAfterPaste).Span.ToTextSpan();

            var originalStringContentsAfterPaste = snapshotAfterPaste.AsText().ToString(spanAfterPaste);
            var newStringContentsAfterEdit = newTextAfterChanges.ToString(spanAfterPaste);

            return originalStringContentsAfterPaste == newStringContentsAfterEdit;
        }

        /// <summary>
        /// Returns the <see cref="LiteralExpressionSyntax"/> or <see cref="InterpolatedStringExpressionSyntax"/> if the
        /// selections were all contained within a single literal in a compatible fashion.  For interpolated strings,
        /// all the selections must be in the same <see cref="SyntaxKind.InterpolatedStringTextToken"/> token.
        /// </summary>
        private static bool AllChangesInSameStringToken(
            SyntaxNode root,
            SourceText text,
            NormalizedSnapshotSpanCollection selectionsBeforePaste,
            [NotNullWhen(true)] out ExpressionSyntax? stringExpression)
        {
            // First, try to see if all the selections are at least contained within a single string literal expression.
            stringExpression = FindContainingStringExpression(root, selectionsBeforePaste);
            if (stringExpression == null)
                return false;

            // Now, given that string expression, find the inside 'text' spans of the expression.  These are the parts
            // of the literal between the quotes.  It does not include the interpolation holes in an interpolated
            // string.  These spans may be empty (for an empty string, or empty text gap between interpolations).
            var contentSpans = GetTextContentSpans(text, stringExpression);

            // Now ensure that all the selections are contained within a single content span.
            int? spanIndex = null;
            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var currentIndex = contentSpans.BinarySearch(
                    snapshotSpan.Span.Start,
                    static (ts, pos) =>
                    {
                        if (ts.IntersectsWith(pos))
                            return 0;

                        if (ts.End < pos)
                            return -1;

                        return 1;
                    });

                if (currentIndex < 0)
                    return false;

                spanIndex ??= currentIndex;
                if (spanIndex != currentIndex)
                    return false;
            }

            return true;
        }

        private abstract class AbstractPasteProcessor
        {
            protected readonly ITextView TextView;
            protected readonly ITextBuffer SubjectBuffer;

            protected readonly ITextSnapshot SnapshotBeforePaste;
            protected readonly ITextSnapshot SnapshotAfterPaste;

            protected readonly Document DocumentBeforePaste;
            protected readonly Document DocumentAfterPaste;

            protected readonly ExpressionSyntax StringExpressionBeforePaste;

            protected readonly INormalizedTextChangeCollection Changes;
            protected readonly string NewLine;

            protected AbstractPasteProcessor(
                ITextView textView,
                ITextBuffer subjectBuffer,
                ITextSnapshot snapshotBeforePaste,
                ITextSnapshot snapshotAfterPaste,
                Document documentBeforePaste,
                Document documentAfterPaste,
                ExpressionSyntax stringExpressionBeforePaste)
            {
                TextView = textView;
                SubjectBuffer = subjectBuffer;

                SnapshotBeforePaste = snapshotBeforePaste;
                SnapshotAfterPaste = snapshotAfterPaste;

                DocumentBeforePaste = documentBeforePaste;
                DocumentAfterPaste = documentAfterPaste;

                StringExpressionBeforePaste = stringExpressionBeforePaste;

                Changes = snapshotBeforePaste.Version.Changes;
                NewLine = textView.Options.GetNewLineCharacter();
            }

            /// <summary>
            /// Returns true if the paste resulted in legal code for the string literal.  The string literal is
            /// considered legal if it has the same span as the original string (adjusted as per the edit) and that
            /// there are no errors in it.  For this purposes of this check, errors in interpolation holes are not
            /// considered.  We only care about the textual content of the string.
            /// </summary>
            protected bool PasteWasSuccessful(CancellationToken cancellationToken)
            {
                // try to find the same token after the paste.  If it's got no errors, and still ends at the same expected
                // location, then it looks like what was pasted was entirely legal and should probably not be touched.

                var documentAfterPaste = SnapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
                Contract.ThrowIfNull(documentAfterPaste);
                var rootAfterPaste = documentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

                var stringExpressionAfterPaste = FindContainingStringExpression(rootAfterPaste, StringExpressionBeforePaste.SpanStart);
                if (stringExpressionAfterPaste == null)
                    return false;

                if (ContainsError(stringExpressionAfterPaste))
                    return false;

                var trackingSpan = SnapshotBeforePaste.CreateTrackingSpan(
                    StringExpressionBeforePaste.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
                var spanAfterPaste = trackingSpan.GetSpan(SnapshotAfterPaste).Span.ToTextSpan();
                return spanAfterPaste == stringExpressionAfterPaste.Span;
            }
        }

        private class UnknownSourcePasteProcessor : AbstractPasteProcessor
        {
            public UnknownSourcePasteProcessor(
                ITextView textView,
                ITextBuffer subjectBuffer,
                ITextSnapshot snapshotBeforePaste,
                ITextSnapshot snapshotAfterPaste,
                Document documentBeforePaste,
                Document documentAfterPaste,
                ExpressionSyntax stringExpressionBeforePaste)
                : base(textView, subjectBuffer, snapshotBeforePaste, snapshotAfterPaste, documentBeforePaste, documentAfterPaste, stringExpressionBeforePaste)
            {
            }

            public ImmutableArray<TextChange> GetTextChanges(CancellationToken cancellationToken)
            {
                // Check for certain things we always think we should escape.
                if (!ShouldAlwaysEscapeTextFromUnknownSource())
                {
                    // If the pasting was successful, then no need to change anything.
                    if (PasteWasSuccessful(cancellationToken))
                        return default;
                }

                // Ok, the user pasted text that couldn't cleanly be added to this token without issue. Repaste the
                // contents, but this time properly escapes/manipulated so that it follows the rule of the particular token
                // kind.
                return GetAppropriateTextChanges(cancellationToken);
            }

            private bool ShouldAlwaysEscapeTextFromUnknownSource()
            {
                if (StringExpressionBeforePaste is LiteralExpressionSyntax literal)
                {
                    // Pasting a control character into a normal string literal is normally not desired.  So even if this
                    // is legal, we still escape the contents to make the pasted code clear.
                    if (literal.Token.IsRegularStringLiteral() && ContainsControlCharacter(Changes))
                        return true;

                    // Always assume passing into a raw string needs adjustment.
                    return IsRawStringLiteral(literal);
                }
                else if (StringExpressionBeforePaste is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    // Pasting a control character into a normal string literal is normally not desired.  So even if this
                    // is legal, we still escape the contents to make the pasted code clear.
                    if (interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken) && ContainsControlCharacter(changes))
                        return true;

                    // Always assume passing into a raw string needs adjustment.
                    return IsRawStringLiteral(interpolatedString);
                }

                throw ExceptionUtilities.UnexpectedValue(StringExpressionBeforePaste);
            }

            private ImmutableArray<TextChange> GetAppropriateTextChanges(CancellationToken cancellationToken)
            {
                // For pastes into non-raw strings, we can just determine how the change should be escaped in-line at that
                // same location the paste originally happened at.  For raw-strings things get more complex as we have to
                // deal with things like indentation and potentially adding newlines to make things legal.
                if (StringExpressionBeforePaste is LiteralExpressionSyntax literalExpression)
                {
                    if (literalExpression.Token.Kind() == SyntaxKind.StringLiteralToken)
                        return GetEscapedTextChangesForNonRawStringLiteral(literalExpression.Token.IsVerbatimStringLiteral());

                    if (literalExpression.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
                        return GetTextChangesForRawStringLiteral(literalExpression, cancellationToken);
                }
                else if (StringExpressionBeforePaste is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedStringStartToken)
                        return GetEscapedTextChangesForNonRawStringLiteral(isVerbatim: false);

                    if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
                        return GetEscapedTextChangesForNonRawStringLiteral(isVerbatim: true);

                    // if (interpolatedString.StringStartToken.Kind() )
                }

                Debug.Fail("Unhandled case: " + StringExpressionBeforePaste.Kind());
                return default;
            }

            private ImmutableArray<TextChange> GetEscapedTextChangesForNonRawStringLiteral(
                bool isVerbatim)
            {
                using var _ = ArrayBuilder<TextChange>.GetInstance(out var textChanges);

                foreach (var change in Changes)
                    textChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), EscapeForNonRawStringLiteral(isVerbatim, change.NewText)));

                return textChanges.ToImmutable();
            }
        }

#if false
            private bool PastedTextEqualsLastCopiedText(ITextBuffer subjectBuffer)
        {
            // If we have no history of any copied text, then there's nothing in the past we can compare to.
            if (_lastSelectedSpans == null)
                return false;

            var copiedSpans = _lastSelectedSpans;
            var pastedChanges = subjectBuffer.CurrentSnapshot.Version.Changes;

            // If we don't have any actual changes to compare, we can't consider these the same.
            if (copiedSpans.Count == 0 || pastedChanges.Count == 0)
                return false;

            // Both the copied and pasted data is normalized.  So we should be able to compare counts to see
            // if they look the same.
            if (copiedSpans.Count != pastedChanges.Count)
                return false;

            // Validate each copied span from the source matches what was pasted into the destination.
            for (int i = 0, n = copiedSpans.Count; i < n; i++)
            {
                var copiedSpan = copiedSpans[i];
                var pastedChange = pastedChanges[i];

                if (copiedSpan.Length != pastedChange.NewLength)
                    return false;

                if (copiedSpan.GetText() != pastedChange.NewText)
                    return false;
            }

            return true;
        }
#endif
    }
}
