﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        private CompletionProvider GetCompletionProvider(CompletionItem item)
        {
            var completionService = this.GetCompletionService() as CompletionServiceWithProviders;
            if (completionService != null)
            {
                return completionService.GetProvider(item);
            }

            return null;
        }

        private void Commit(PresentationItem item, Model model, char? commitChar)
        {
            AssertIsForeground();

            // We could only be called if we had a model at this point.
            Contract.ThrowIfNull(model);

            // Now that we've captured the model at this point, we can stop ourselves entirely.  
            // This is also desirable as we may have reentrancy problems when we call into 
            // custom commit completion providers.  I.e. if the custom provider moves the caret,
            // then we do not want to process that move as it may put us into an unexpected state.
            //
            // TODO(cyrusn): We still have a general reentrancy problem where calling into a custom
            // commit provider (or just calling into the editor) may cause something to call back
            // into us.  However, for now, we just hope that no such craziness will occur.
            this.StopModelComputation();

            Commit(item, model, commitChar, CancellationToken.None);
        }

        private void Commit(PresentationItem item, Model model, char? commitChar, CancellationToken cancellationToken)
        {
            var textChangesInTriggerSnapshot = ImmutableArray<TextChange>.Empty;

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
                var provider = GetCompletionProvider(item.Item) as ICustomCommitCompletionProvider;
                if (provider == null)
                {
                    var viewBuffer = this.TextView.TextBuffer;
                    var triggerSnapshotDocument = model.TriggerSnapshot.AsText().GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                    // Get the desired text changes for this item. Note that these changes are
                    // specified in terms of the trigger snapshot.
                    var completionService = CompletionService.GetService(triggerSnapshotDocument);
                    var triggerSnapshotChange = completionService.GetChangeAsync(
                        triggerSnapshotDocument, item.Item, commitChar, cancellationToken).WaitAndGetResult(cancellationToken);
                    textChangesInTriggerSnapshot = triggerSnapshotChange.TextChanges;

                    // Use character based diffing here to avoid overwriting the commit character placed into the editor.
                    var editOptions = new EditOptions(new StringDifferenceOptions
                    {
                        DifferenceType = StringDifferenceTypes.Character,
                        IgnoreTrimWhiteSpace = EditOptions.DefaultMinimalChange.DifferenceOptions.IgnoreTrimWhiteSpace
                    });

                    var textChangesInCurrentViewSnapshot = new List<TextChange>();

                    foreach (var triggerSnapshotTextChange in textChangesInTriggerSnapshot)
                    {
                        // Try mapping the item span against the trigger snapshot to the ViewBuffer's trigger snapshot.
                        // Then map that forward to the ViewBuffer's current snapshot.
                        var originalSpanInView = model.GetViewBufferSpan(triggerSnapshotTextChange.Span).TextSpan.ToSnapshotSpan(model.ViewTriggerSnapshot);
                        var originalViewSpanTranslatedToCurrentViewSnapshot = originalSpanInView.TranslateTo(TextView.TextBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                        textChangesInCurrentViewSnapshot.Add(new TextChange(originalViewSpanTranslatedToCurrentViewSnapshot.Span.ToTextSpan(), triggerSnapshotTextChange.NewText));
                    }

                    // Note: we currently create the edit on the textview's buffer.  This is 
                    // necessary as our own 
                    using (var textEdit = TextView.TextBuffer.CreateEdit(editOptions, reiteratedVersionNumber: null, editTag: null))
                    {
                        for (int iChange = 0; iChange < textChangesInCurrentViewSnapshot.Count; iChange++)
                        { 
                            var textChangeInView = textChangesInCurrentViewSnapshot[iChange];
                            var isFirst = iChange == 0;
                            var isLast = iChange == textChangesInCurrentViewSnapshot.Count - 1;

                            // add commit char to end of last change if not already included 
                            if (isLast && !triggerSnapshotChange.IncludesCommitCharacter && commitChar.HasValue)
                            {
                                textChangeInView = new TextChange(
                                    textChangeInView.Span, textChangeInView.NewText + commitChar.Value);
                            }

                            // In order to play nicely with automatic brace completion, we need to 
                            // not touch the opening paren. We'll check our span and textchange 
                            // for ( and adjust them accordingly if we find them.

                            // all this is needed since we don't use completion set mechanism provided by VS but we implement everything ourselves.
                            // due to that, existing brace completion engine in editor that should take care of interaction between brace completion
                            // and intellisense doesn't work for us. so we need this kind of workaround to support it nicely.
                            bool textChanged;
                            string newText = textChangeInView.NewText;

                            if (isFirst)
                            {
                                newText = AdjustFirstText(textChangeInView);
                            }

                            var currentSpan = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot,
                                textChangeInView.Span.ToSpan());
                            if (isLast)
                            {
                                newText = AdjustLastText(newText, commitChar.GetValueOrDefault(), out textChanged);
                                currentSpan = AdjustLastSpan(currentSpan, commitChar.GetValueOrDefault(), textChanged);
                            }

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

                            textEdit.Replace(currentSpan, newText);
                        }

                        textEdit.Apply();
                    }

                    // adjust the caret position if requested by completion service
                    if (triggerSnapshotChange.NewPosition != null)
                    {
                        var target = new SnapshotPoint(this.SubjectBuffer.CurrentSnapshot, triggerSnapshotChange.NewPosition.Value);
                        this.TextView.TryMoveCaretToAndEnsureVisible(target);
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
                    provider.Commit(item.Item, this.TextView, this.SubjectBuffer, model.TriggerSnapshot, commitChar);
                    transaction?.Complete();
                }
            }

            var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var formattingService = document.GetLanguageService<IEditorFormattingService>();

            var commitCharTriggersFormatting = commitChar != null &&
                    (formattingService?.SupportsFormattingOnTypedCharacter(document, commitChar.GetValueOrDefault())
                     ?? false);

            if (formattingService != null && (item.Item.Rules.FormatOnCommit || commitCharTriggersFormatting))
            {
                // Formatting the completion item affected span is done as a separate transaction because this gives the user
                // the flexibility to undo the formatting but retain the changes associated with the completion item
                using (var formattingTransaction = _undoHistoryRegistry.GetHistory(this.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.IntelliSenseCommitFormatting))
                {
                    var caretPoint = this.TextView.GetCaretPoint(this.SubjectBuffer);
                    IList<TextChange> changes = null;

                    if (commitCharTriggersFormatting && caretPoint.HasValue)
                    {
                        // if the commit character is supported by formatting service, then let the formatting service
                        // find the appropriate range to format.
                        changes = formattingService.GetFormattingChangesAsync(document, commitChar.Value, caretPoint.Value.Position, cancellationToken).WaitAndGetResult(cancellationToken);
                    }
                    else if (textChangesInTriggerSnapshot.Length > 0)
                    {
                        // if this is not a supported trigger character for formatting service (space or tab etc.)
                        // then format the span of the textchange.
                        var totalSpan = TextSpan.FromBounds(textChangesInTriggerSnapshot.Min(c => c.Span.Start), textChangesInTriggerSnapshot.Max(c => c.Span.End));
                        changes = formattingService.GetFormattingChangesAsync(document, totalSpan, cancellationToken).WaitAndGetResult(cancellationToken);
                    }

                    if (changes != null && !changes.IsEmpty())
                    {
                        document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
                    }

                    formattingTransaction.Complete();
                }
            }

            // Let the completion rules know that this item was committed.
            this.MakeMostRecentItem(item.Item.DisplayText);
        }

        private SnapshotSpan AdjustLastSpan(SnapshotSpan currentSpan, char commitChar, bool textChanged)
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

        private string AdjustFirstText(TextChange textChange)
        { 
            var newText = textChange.NewText;

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
                return whitespace + newText;
            }

            return newText;
        }

        private string AdjustLastText(string text, char commitChar, out bool textAdjusted)
        {
            var finalText = this.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.AutomaticPairCompletion)
                ? text.TrimEnd(commitChar)
                : text;

            // set whether text has changed or not
            textAdjusted = finalText != text;

            return finalText;
        }
    }
}
