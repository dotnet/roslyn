// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    using static StringCopyPasteHelpers;

    /// <summary>
    /// Command handler that both tracks 'copy' commands within VS to see what text the user copied (and from where),
    /// but also then handles pasting that text back in a sensible fashion (e.g. escaping/unescaping/wrapping/indenting)
    /// inside a string-literal.  Can also handle pasting code from unknown sources as well, though heuristics must be
    /// applied in that case to make a best effort guess as to what the original text meant and how to preserve that
    /// in the final context.
    /// </summary>
    /// <remarks>
    /// Because we are revising what the normal editor does, we follow the standard behavior of first allowing the
    /// editor to process paste commands, and then adding our own changes as an edit after that.  That way if the user
    /// doesn't want the change we made, they can always undo to get the prior paste behavior.
    /// </remarks>
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
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            var selectionsBeforePaste = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
            var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;

            // Always let the real paste go through.  That way we always have a version of the document that doesn't
            // include our changes that we can undo back to.
            nextCommandHandler();

            // If the user has the option off, then don't bother doing anything once we've sent the paste through.
            if (!_globalOptions.GetOption(FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste, LanguageNames.CSharp))
                return;

            // if we're not even sure where the user caret/selection is on this buffer, we can't proceed.
            if (selectionsBeforePaste.Count == 0)
                return;

            var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;

            // If there were multiple changes that already happened, then don't make any changes.  Some other component
            // already did something advanced.
            if (snapshotAfterPaste.Version != snapshotBeforePaste.Version.Next)
                return;

            // Have to even be in a C# doc to be able to have special space processing here.

            var documentBeforePaste = snapshotBeforePaste.GetOpenDocumentInCurrentContextWithChanges();
            var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
            if (documentBeforePaste == null || documentAfterPaste == null)
                return;

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

            var rootBeforePaste = documentBeforePaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

            // When pasting, only do anything special if the user selections were entirely inside a single string
            // literal token.  Otherwise, we have a multi-selection across token kinds which will be extremely 
            // complex to try to reconcile.
            var stringExpressionBeforePaste = TryGetCompatibleContainingStringExpression(
                rootBeforePaste, snapshotBeforePaste.AsText(), selectionsBeforePaste);
            if (stringExpressionBeforePaste == null)
                return;

            // TODO: add support for pasting content that came from within the editor.  We can exactly know what that
            // content meant, and how to insert it into our string expression.

            var pasteWasSuccessful = PasteWasSuccessful(
                snapshotBeforePaste, snapshotAfterPaste, documentAfterPaste, stringExpressionBeforePaste, cancellationToken);

            var indentationOptions = documentBeforePaste.GetIndentationOptionsAsync(_globalOptions, cancellationToken).WaitAndGetResult(cancellationToken);
            var processor = new UnknownSourcePasteProcessor(
                snapshotBeforePaste,
                snapshotAfterPaste,
                documentBeforePaste,
                documentAfterPaste,
                stringExpressionBeforePaste,
                textView.Options.GetNewLineCharacter(),
                indentationOptions,
                pasteWasSuccessful);

            var textChanges = processor.GetEdits(cancellationToken);

            // If we didn't get any viable changes back, don't do anything.
            if (textChanges.IsDefaultOrEmpty)
                return;

            var newTextAfterChanges = snapshotBeforePaste.AsText().WithChanges(textChanges);

            // If we end up making the same changes as what the paste did, then no need to proceed.
            if (ContentsAreSame(snapshotBeforePaste, snapshotAfterPaste, stringExpressionBeforePaste, newTextAfterChanges))
                return;

            // Create two edits to make the change.  The first restores the buffer to the original snapshot (effectively
            // undoing the first set of changes).  Then the second actually applies the change.
            //
            // Do this as direct edits, passing 'EditOptions.None' for the options, as we want to control the edits
            // precisely and don't want any strange interpretation of where the caret should end up.  Other options
            // (like DefaultMinimalChange) will attempt to diff/merge edits oddly sometimes which can lead the caret
            // ending up before/after some merged change, which will no longer match the behavior of precise pastes.
            //
            // Wrap this all as a transaction so that these two edits appear to be one single change.  This also allows
            // the user to do a single 'undo' that gets them back to the original paste made at the start of this
            // method.

            using var transaction = new CaretPreservingEditTransaction(
                CSharpEditorResources.Fixing_string_literal_after_paste,
                textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            {
                var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
                foreach (var change in snapshotBeforePaste.Version.Changes)
                    edit.Replace(change.NewSpan, change.OldText);
                edit.Apply();
            }

            {
                var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
                foreach (var selection in selectionsBeforePaste)
                    edit.Replace(selection.Span, "");

                foreach (var change in textChanges)
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                edit.Apply();
            }

            transaction.Complete();
        }

        /// <summary>
        /// Returns true if the paste resulted in legal code for the string literal.  The string literal is
        /// considered legal if it has the same span as the original string (adjusted as per the edit) and that
        /// there are no errors in it.  For this purposes of this check, errors in interpolation holes are not
        /// considered.  We only care about the textual content of the string.
        /// </summary>
        internal static bool PasteWasSuccessful(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            Document documentAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            CancellationToken cancellationToken)
        {
            var rootAfterPaste = documentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var stringExpressionAfterPaste = FindContainingSupportedStringExpression(rootAfterPaste, stringExpressionBeforePaste.SpanStart);
            if (stringExpressionAfterPaste == null)
                return false;

            if (ContainsError(stringExpressionAfterPaste))
                return false;

            var spanAfterPaste = MapSpan(stringExpressionBeforePaste.Span, snapshotBeforePaste, snapshotAfterPaste);
            return spanAfterPaste == stringExpressionAfterPaste.Span;
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
            // We ended up with documents of different length after we escaped/manipulated the pasted text.  So the 
            // contents are definitely not the same.
            if (newTextAfterChanges.Length != snapshotAfterPaste.Length)
                return false;

            var spanAfterPaste = MapSpan(stringExpressionBeforePaste.Span, snapshotBeforePaste, snapshotAfterPaste);

            var originalStringContentsAfterPaste = snapshotAfterPaste.AsText().GetSubText(spanAfterPaste);
            var newStringContentsAfterEdit = newTextAfterChanges.GetSubText(spanAfterPaste);

            return originalStringContentsAfterPaste.ContentEquals(newStringContentsAfterEdit);
        }

        /// <summary>
        /// Returns the <see cref="LiteralExpressionSyntax"/> or <see cref="InterpolatedStringExpressionSyntax"/> if the
        /// selections were all contained within a single literal in a compatible fashion.  This means all the
        /// selections have to start/end in a content-span portion of the literal.  For example, if we paste into an
        /// interpolated string and have half of the selection outside an interpolation and half inside, we don't do
        /// anything special as trying to correct in this scenario is too difficult.
        /// </summary>
        private static ExpressionSyntax? TryGetCompatibleContainingStringExpression(
            SyntaxNode root,
            SourceText text,
            NormalizedSnapshotSpanCollection selectionsBeforePaste)
        {
            // First, try to see if all the selections are at least contained within a single string literal expression.
            var stringExpression = FindCommonContainingStringExpression(root, selectionsBeforePaste);
            if (stringExpression == null)
                return null;

            // Now, given that string expression, find the inside 'text' spans of the expression.  These are the parts
            // of the literal between the quotes.  It does not include the interpolation holes in an interpolated
            // string.  These spans may be empty (for an empty string, or empty text gap between interpolations).
            var contentSpans = GetTextContentSpans(text, stringExpression, out _, out _);

            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var startIndex = contentSpans.BinarySearch(snapshotSpan.Span.Start, FindIndex);
                var endIndex = contentSpans.BinarySearch(snapshotSpan.Span.End, FindIndex);

                if (startIndex < 0 || endIndex < 0)
                    return null;
            }

            return stringExpression;

            static int FindIndex(TextSpan span, int position)
            {
                if (span.IntersectsWith(position))
                    return 0;

                if (span.End < position)
                    return -1;

                return 1;
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
