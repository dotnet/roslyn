// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
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

        private void CommitOnNonTypeChar(
            PresentationItem item, Model model)
        {
            Commit(item, model, commitChar: null, initialTextSnapshot: null, nextHandler: null);
        }

        private void Commit(
            PresentationItem item, Model model, char? commitChar,
            ITextSnapshot initialTextSnapshot, Action nextHandler)
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

            CompletionChange completionChange;
            using (var transaction = new CaretPreservingEditTransaction(
                EditorFeaturesResources.IntelliSense, TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
            {
                // We want to merge with any of our other programmatic edits (e.g. automatic brace completion)
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;

                var provider = GetCompletionProvider(item.Item) as ICustomCommitCompletionProvider;
                if (provider != null)
                {
                    provider.Commit(item.Item, this.TextView, this.SubjectBuffer, model.TriggerSnapshot, commitChar);
                }
                else
                {
                    // Right before calling Commit, we may have passed the commitChar through to the
                    // editor.  That was so that undoing completion will get us back to the state we
                    // we would be in if completion had done nothing.  However, now that we're going
                    // to actually commit, we want to roll back to where we were before we pushed
                    // commit character into the buffer.  This has multiple benefits:
                    //
                    //   1) the buffer is in a state we expect it to be in.  i.e. we don't have to
                    //      worry about what might have happened (like brace-completion) when the
                    //      commit char was inserted.
                    //   2) after we commit the item, we'll pass the commit character again into the
                    //      buffer (unless the items asks us not to).  By doing this, we can make sure
                    //      that things like brace-completion or formatting trigger as we expect them
                    //      to.
                    var characterWasSentIntoBuffer = commitChar != null &&
                                                     initialTextSnapshot.Version.VersionNumber != this.SubjectBuffer.CurrentSnapshot.Version.VersionNumber;
                    if (characterWasSentIntoBuffer)
                    {
                        RollbackToBeforeTypeChar(initialTextSnapshot);
                    }

                    // Now, get the change the item wants to make.  Note that the change will be relative
                    // to the initial snapshot/document the item was triggered from.  We'll map that change
                    // forward, then apply it to our current snapshot.
                    var triggerDocument = model.TriggerDocument;
                    var triggerSnapshot = model.TriggerSnapshot;

                    var completionService = CompletionService.GetService(triggerDocument);
                    completionChange = completionService.GetChangeAsync(
                        triggerDocument, item.Item, commitChar, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    var textChange = completionChange.TextChange;

                    var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
                    var mappedSpan = triggerSnapshotSpan.TranslateTo(
                        this.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                    // Now actually make the text change to the document.
                    using (var textEdit = this.SubjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
                    {
                        var adjustedNewText = AdjustForVirtualSpace(textChange);

                        textEdit.Replace(mappedSpan.Span, adjustedNewText);
                        textEdit.Apply();
                    }

                    // adjust the caret position if requested by completion service
                    if (completionChange.NewPosition != null)
                    {
                        TextView.Caret.MoveTo(new SnapshotPoint(
                            this.SubjectBuffer.CurrentSnapshot, completionChange.NewPosition.Value));
                    }

                    // Now, pass along the commit character unless the completion item said not to
                    if (characterWasSentIntoBuffer && !completionChange.IncludesCommitCharacter)
                    {
                        nextHandler();
                    }

                    // If the insertion is long enough, the caret will scroll out of the visible area.
                    // Re-center the view.
                    this.TextView.Caret.EnsureVisible();
                }

                transaction.Complete();
            }

            // Let the completion rules know that this item was committed.
            this.MakeMostRecentItem(item.Item.DisplayText);
        }

        private void RollbackToBeforeTypeChar(ITextSnapshot initialTextSnapshot)
        {
            // Get all the versions from the initial text snapshot (before we passed the
            // commit character down) to the current snapshot we're at.
            var versions = GetVersions(initialTextSnapshot, this.SubjectBuffer.CurrentSnapshot).ToList();

            // Un-apply the edits. 
            for (var i = versions.Count - 1; i >= 0; i--)
            {
                var version = versions[i];
                using (var textEdit = this.SubjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
                {
                    foreach (var change in version.Changes)
                    {
                        textEdit.Replace(change.NewSpan, change.OldText);
                    }

                    textEdit.Apply();
                }
            }
        }

        private IEnumerable<ITextVersion> GetVersions(
            ITextSnapshot initialTextSnapshot, ITextSnapshot currentSnapshot)
        {
            var version = initialTextSnapshot.Version;
            while (version != null && version.VersionNumber != currentSnapshot.Version.VersionNumber)
            {
                yield return version;
                version = version.Next;
            }
        }

        private string AdjustForVirtualSpace(TextChange textChange)
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
    }
}