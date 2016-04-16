// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        private void Commit(CompletionItem item, TextChange textChange, Model model, char? commitChar)
        {
            AssertIsForeground();

            // We could only be called if we had a model at this point.
            Contract.ThrowIfNull(model);

            item = Controller.GetExternallyUsableCompletionItem(item);

            // Now that we've captured the model at this point, we can stop ourselves entirely.  
            // This is also desirable as we may have reentrancy problems when we call into 
            // custom commit completion providers.  I.e. if the custom provider moves the caret,
            // then we do not want to process that move as it may put us into an unexpected state.
            //
            // TODO(cyrusn): We still have a general reentrancy problem where calling into a custom
            // commit provider (or just calling into the editor) may cause something to call back
            // into us.  However, for now, we just hope that no such craziness will occur.
            this.StopModelComputation();

            // NOTE(cyrusn): It is intentional that we get the undo history for the
            // surface buffer and not the subject buffer.
            // There have been some watsons where the ViewBuffer hadn't been registered,
            // so use TryGetHistory instead.
            ITextUndoHistory undoHistory;
            _undoHistoryRegistry.TryGetHistory(this.TextView.TextBuffer, out undoHistory);

            using (var transaction = undoHistory?.CreateTransaction(EditorFeaturesResources.IntelliSense))
            {
                // We want to merge with any of our other programmatic edits (e.g. automatic brace completion)
                if (transaction != null)
                {
                    transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;
                }

                // Check if the provider wants to perform custom commit itself.  Otherwise we will
                // handle things.
                var provider = item.CompletionProvider as ICustomCommitCompletionProvider;
                if (provider == null)
                {
                    var viewBuffer = this.TextView.TextBuffer;

                    // Use character based diffing here to avoid overwriting the commit character placed into the editor.
                    var editOptions = new EditOptions(new StringDifferenceOptions
                    {
                        DifferenceType = StringDifferenceTypes.Character,
                        IgnoreTrimWhiteSpace = EditOptions.DefaultMinimalChange.DifferenceOptions.IgnoreTrimWhiteSpace
                    });

                    using (var textEdit = viewBuffer.CreateEdit(editOptions, reiteratedVersionNumber: null, editTag: null))
                    {
                        var viewSpan = model.GetSubjectBufferFilterSpanInViewBuffer(textChange.Span);
                        var currentSpan = model.GetCurrentSpanInSnapshot(viewSpan, viewBuffer.CurrentSnapshot);

                        // In order to play nicely with automatic brace completion, we need to 
                        // not touch the opening paren. We'll check our span and textchange 
                        // for ( and adjust them accordingly if we find them.

                        // all this is needed since we don't use completion set mechanism provided by VS but we implement everything ourselves.
                        // due to that, existing brace completion engine in editor that should take care of interaction between brace completion
                        // and intellisense doesn't work for us. so we need this kind of workaround to support it nicely.
                        bool textChanged;
                        var finalText = GetFinalText(textChange, commitChar.GetValueOrDefault(), out textChanged);
                        currentSpan = GetFinalSpan(currentSpan, commitChar.GetValueOrDefault(), textChanged);

                        var caretPoint = this.TextView.GetCaretPoint(this.SubjectBuffer);
                        var virtualCaretPoint = this.TextView.GetVirtualCaretPoint(this.SubjectBuffer);

                        if (caretPoint.HasValue && virtualCaretPoint.HasValue)
                        {
                            // TODO(dustinca): We need to call a different API here. TryMoveCaretToAndEnsureVisible might center within the view.
                            this.TextView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(caretPoint.Value));
                        }

                        caretPoint = this.TextView.GetCaretPoint(this.SubjectBuffer);

                        // Now that we're doing character level diffing, we need to move the caret to the end of 
                        // the span being replaced. Otherwise, we can replace M|ai with Main and wind up with 
                        // M|ain, since character based diffing makes that quite legit.
                        if (caretPoint.HasValue)
                        {
                            var endInSubjectBuffer = this.TextView.BufferGraph.MapDownToBuffer(currentSpan.End, PointTrackingMode.Positive, caretPoint.Value.Snapshot.TextBuffer, PositionAffinity.Predecessor);
                            if (caretPoint.Value < endInSubjectBuffer)
                            {
                                this.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(currentSpan.Snapshot.TextBuffer.CurrentSnapshot, currentSpan.End.Position));
                            }
                        }

                        textEdit.Replace(currentSpan, finalText);
                        textEdit.Apply();
                    }

                    // We've manipulated the caret position in order to generate the correct edit. However, 
                    // if the insertion is long enough, the caret will scroll out of the visible area.
                    // Re-center the view.
                    using (var textEdit = viewBuffer.CreateEdit(editOptions, reiteratedVersionNumber: null, editTag: null))
                    {
                        var caretPoint = this.TextView.GetCaretPoint(this.SubjectBuffer);
                        if (caretPoint.HasValue)
                        {
                            this.TextView.Caret.EnsureVisible();
                        }
                    }

                    transaction?.Complete();
                }
                else
                {
                    // Let the provider handle this.
                    provider.Commit(item, this.TextView, this.SubjectBuffer, model.TriggerSnapshot, commitChar);
                    transaction?.Complete();
                }
            }

            var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var formattingService = document.GetLanguageService<IEditorFormattingService>();

            var commitCharTriggersFormatting = commitChar != null &&
                    (formattingService?.SupportsFormattingOnTypedCharacter(document, commitChar.GetValueOrDefault())
                     ?? false);

            if (formattingService != null && (item.ShouldFormatOnCommit || commitCharTriggersFormatting))
            {
                // Formatting the completion item affected span is done as a separate transaction because this gives the user
                // the flexibility to undo the formatting but retain the changes associated with the completion item
                using (var formattingTransaction = _undoHistoryRegistry.GetHistory(this.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.IntelliSenseCommitFormatting))
                {
                    var caretPoint = this.TextView.GetCaretPoint(this.SubjectBuffer);
                    IList<TextChange> changes;
                    if (commitCharTriggersFormatting && caretPoint.HasValue)
                    {
                        // if the commit character is supported by formatting service, then let the formatting service
                        // find the appropriate range to format.
                        changes = formattingService.GetFormattingChangesAsync(document, commitChar.Value, caretPoint.Value.Position, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    }
                    else
                    {
                        // if this is not a supported trigger character for formatting service (space or tab etc.)
                        // then format the span of the textchange.
                        changes = formattingService.GetFormattingChangesAsync(document, textChange.Span, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    }

                    if (changes != null && !changes.IsEmpty())
                    {
                        document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, CancellationToken.None);
                    }

                    formattingTransaction.Complete();
                }
            }

            // Let the completion rules know that this item was committed.
            GetCompletionRules().CompletionItemCommitted(item);
        }

        private SnapshotSpan GetFinalSpan(SnapshotSpan currentSpan, char commitChar, bool textChanged)
        {
            var currentSpanText = currentSpan.GetText();
            if (currentSpan.Length > 0 && this.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.AutomaticPairCompletion))
            {
                if (currentSpanText[currentSpanText.Length - 1] == commitChar)
                {
                    return new SnapshotSpan(currentSpan.Start, currentSpan.Length - 1);
                }

                // looks like auto insertion happened. find right span to replace
                if (textChanged)
                {
                    var index = currentSpanText.LastIndexOf(commitChar);
                    if (index >= 0)
                    {
                        return new SnapshotSpan(currentSpan.Start, index);
                    }
                }
            }

            return currentSpan;
        }

        private string GetFinalText(TextChange textChange, char commitChar, out bool textAdjusted)
        {
            var applicableText = this.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.AutomaticPairCompletion)
                ? textChange.NewText.TrimEnd(commitChar)
                : textChange.NewText;

            // set whether text has changed or not
            textAdjusted = applicableText != textChange.NewText;

            var caretPoint = this.TextView.Caret.Position.BufferPosition;
            var virtualCaretPoint = this.TextView.Caret.Position.VirtualBufferPosition;

            if (textChange.Span.IsEmpty &&
                textChange.Span.Start == caretPoint &&
                virtualCaretPoint.IsInVirtualSpace)
            {
                // They're in virtual space and the text change is specified against the cursor
                // position that isn't in virtual space.  In this case, add the virtual spaces to the
                // thing we're adding.
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(this.TextView);
                var whitespace = editorOperations.GetWhitespaceForVirtualSpace(virtualCaretPoint);
                return whitespace + applicableText;
            }

            return applicableText;
        }
    }
}
