// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        private CompletionProvider GetCompletionProvider(CompletionItem item)
        {
            if (this.GetCompletionService() is CompletionServiceWithProviders completionService)
            {
                return completionService.GetProvider(item);
            }

            return null;
        }

        private void CommitOnNonTypeChar(
            CompletionItem item, Model model)
        {
            Commit(item, model, commitChar: null, initialTextSnapshot: null, nextHandler: null);
        }

        private void Commit(
            CompletionItem item, Model model, char? commitChar,
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
            this.DismissSessionIfActive();

            CompletionChange completionChange;
            using (var transaction = CaretPreservingEditTransaction.TryCreate(
                EditorFeaturesResources.IntelliSense, TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
            {
                if (transaction == null)
                {
                    // This text buffer has no undo history and has probably been unmapped.
                    // (Workflow unmaps its projections when losing focus (such as double clicking the completion list)).
                    // Bail on committing completion because we won't be able to find a Document to update either.

                    return;
                }

                // We want to merge with any of our other programmatic edits (e.g. automatic brace completion)
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;

                if (GetCompletionProvider(item) is ICustomCommitCompletionProvider provider)
                {
                    provider.Commit(item, this.TextView, this.SubjectBuffer, model.TriggerSnapshot, commitChar);
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
                    Contract.ThrowIfNull(completionService, nameof(completionService));

                    completionChange = completionService.GetChangeAsync(
                        triggerDocument, item, model.OriginalList.Span, commitChar, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    var textChange = completionChange.TextChange;

                    var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
                    var mappedSpan = triggerSnapshotSpan.TranslateTo(
                        this.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                    var adjustedNewText = AdjustForVirtualSpace(textChange, this.TextView, _editorOperationsFactoryService);
                    var editOptions = GetEditOptions(mappedSpan, adjustedNewText);

                    // The immediate window is always marked read-only and the language service is
                    // responsible for asking the buffer to make itself writable. We'll have to do that for
                    // commit, so we need to drag the IVsTextLines around, too.
                    // We have to ask the buffer to make itself writable, if it isn't already
                    uint immediateWindowBufferUpdateCookie = 0;
                    if (_isImmediateWindow)
                    {
                        immediateWindowBufferUpdateCookie = ((IDebuggerTextView)TextView).StartBufferUpdate();
                    }

                    // Now actually make the text change to the document.
                    using (var textEdit = this.SubjectBuffer.CreateEdit(editOptions, reiteratedVersionNumber: null, editTag: null))
                    {
                        textEdit.Replace(mappedSpan.Span, adjustedNewText);
                        textEdit.ApplyAndLogExceptions();
                    }

                    if (_isImmediateWindow)
                    {
                        ((IDebuggerTextView)TextView).EndBufferUpdate(immediateWindowBufferUpdateCookie);
                    }

                    // If the completion change requested a new position for the caret to go,
                    // then set the caret to go directly to that point.
                    if (completionChange.NewPosition.HasValue)
                    {
                        SetCaretPosition(desiredCaretPosition: completionChange.NewPosition.Value);
                    }
                    else if (editOptions.ComputeMinimalChange)
                    {
                        // Or, If we're doing a minimal change, then the edit that we make to the 
                        // buffer may not make the total text change that places the caret where we 
                        // would expect it to go based on the requested change. In this case, 
                        // determine where the item should go and set the care manually.

                        // Note: we only want to move the caret if the caret would have been moved 
                        // by the edit.  i.e. if the caret was actually in the mapped span that 
                        // we're replacing.
                        if (TextView.GetCaretPoint(this.SubjectBuffer) is SnapshotPoint caretPositionInBuffer &&
                            mappedSpan.IntersectsWith(caretPositionInBuffer))
                        {
                            SetCaretPosition(desiredCaretPosition: mappedSpan.Start.Position + adjustedNewText.Length);
                        }
                    }

                    // Now, pass along the commit character unless the completion item said not to
                    if (characterWasSentIntoBuffer && !completionChange.IncludesCommitCharacter)
                    {
                        nextHandler();
                    }

                    if (item.Rules.FormatOnCommit)
                    {
                        var spanToFormat = triggerSnapshotSpan.TranslateTo(
                            this.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
                        var document = this.GetDocument();
                        var formattingService = document?.GetLanguageService<IEditorFormattingService>();

                        if (formattingService != null)
                        {
                            var changes = formattingService.GetFormattingChangesAsync(
                                document, spanToFormat.Span.ToTextSpan(), CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                            document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, CancellationToken.None);
                        }
                    }

                    // If the insertion is long enough, the caret will scroll out of the visible area.
                    // Re-center the view.
                    this.TextView.Caret.EnsureVisible();
                }

                transaction.Complete();
                Logger.Log(FunctionId.Intellisense_Completion_Commit, KeyValueLogMessage.NoProperty);
            }

            // Let the completion rules know that this item was committed.
            this.MakeMostRecentItem(item.DisplayText);
        }

        private static string AdjustForVirtualSpace(TextChange textChange, ITextView textView, IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            var newText = textChange.NewText;

            var caretPoint = textView.Caret.Position.BufferPosition;
            var virtualCaretPoint = textView.Caret.Position.VirtualBufferPosition;

            if (textChange.Span.IsEmpty &&
                textChange.Span.Start == caretPoint &&
                virtualCaretPoint.IsInVirtualSpace)
            {
                // They're in virtual space and the text change is specified against the cursor
                // position that isn't in virtual space.  In this case, add the virtual spaces to the
                // thing we're adding.
                var editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
                var whitespace = editorOperations.GetWhitespaceForVirtualSpace(virtualCaretPoint);
                return whitespace + newText;
            }

            return newText;
        }

        private void SetCaretPosition(int desiredCaretPosition)
        {
            // Now, move the caret to the right location.
            var graph = new DisconnectedBufferGraph(this.SubjectBuffer, this.TextView.TextBuffer);
            var viewTextSpan = graph.GetSubjectBufferTextSpanInViewBuffer(new TextSpan(desiredCaretPosition, 0));

            TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, viewTextSpan.TextSpan.Start));
        }

        private EditOptions GetEditOptions(SnapshotSpan spanToReplace, string adjustedNewText)
        {
            if (spanToReplace.GetText() == adjustedNewText)
            {
                // We're replacing the current buffer text with the exact same code.  If 
                // we pass EditOptions.DefaultMinimalChange then no actual buffer change
                // will happen.  That's problematic as it breaks features like brace-matching
                // which want to buffer changes to properly compute their state.  In this
                return EditOptions.None;
            }

            return EditOptions.DefaultMinimalChange;
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
                using var textEdit = this.SubjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);

                foreach (var change in version.Changes)
                {
                    textEdit.Replace(change.NewSpan, change.OldText);
                }

                textEdit.ApplyAndLogExceptions();
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
    }
}
