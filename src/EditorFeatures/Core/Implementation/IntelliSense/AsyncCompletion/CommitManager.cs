// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal sealed class CommitManager : ForegroundThreadAffinitizedObject, IAsyncCompletionCommitManager
    {
        private static readonly AsyncCompletionData.CommitResult CommitResultUnhandled =
            new AsyncCompletionData.CommitResult(isHandled: false, AsyncCompletionData.CommitBehavior.None);

        public IEnumerable<char> PotentialCommitCharacters { get; }

        internal CommitManager(ImmutableArray<char> potentialCommitCharacters, IThreadingContext threadingContext) : base(threadingContext)
        {
            PotentialCommitCharacters = potentialCommitCharacters;
        }

        /// <summary>
        /// The method performs a preliminarily filtering of commit availability.
        /// In case of a doubt, it should respond with true.
        /// We will be able to cancel later in TryCommit based on VSCompletionItem item, e.g. based on CompletionItemRules.
        /// </summary>
        public bool ShouldCommitCompletion(
            IAsyncCompletionSession session,
            SnapshotPoint location,
            char typedChar,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();
            if (session.Properties.TryGetProperty<ImmutableArray<char>>(CompletionSource.ExcludedCommitCharacters, out var excludedCommitCharacter))
            {
                if (excludedCommitCharacter.Contains(typedChar))
                {
                    return false;
                }
            }

            return PotentialCommitCharacters.Contains(typedChar);
        }

        public AsyncCompletionData.CommitResult TryCommit(
            IAsyncCompletionSession session,
            ITextBuffer subjectBuffer,
            VSCompletionItem item,
            char typeChar,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return CommitResultUnhandled;
            }

            var completionService = document.GetLanguageService<CompletionService>();
            if (completionService == null)
            {
                return CommitResultUnhandled;
            }

            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionSource.RoslynItem, out var roslynItem))
            {
                // This isn't an item we provided (e.g. Razor). Let the editor handle it normally.
                return CommitResultUnhandled;
            }

            var filterText = session.ApplicableToSpan.GetText(subjectBuffer.CurrentSnapshot) + typeChar;
            if (Controller.IsFilterCharacter(roslynItem, typeChar, filterText))
            { 
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            var serviceRules = completionService.GetRules();

            // We can be called before for ShouldCommitCompletion. However, that call does not provide rules applied for the completion item.
            // Now we check for the commit charcter in the context of Rules that could change the list of commit characters.

            // Tab, Enter and Null (call invoke commit) are always a commit character. 
            if (typeChar != '\t' && typeChar != '\n' && typeChar != '\0' && !Controller.IsCommitCharacter(serviceRules, roslynItem, typeChar, filterText))
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            if (!item.Properties.TryGetProperty<ITextSnapshot>(CompletionSource.TriggerSnapshot, out var triggerSnapshot))
            {
                return CommitResultUnhandled;
            }

            // Commit with completion serivce assumes that null is provided is case of invoke. VS provides '\0' in the case.
            char? commitChar = typeChar == '\0' ? null : (char?)typeChar;
            var commitBehavior = Commit(
                document, completionService, session.TextView, subjectBuffer, 
                roslynItem, commitChar, triggerSnapshot, serviceRules, filterText, cancellationToken);

            return new AsyncCompletionData.CommitResult(isHandled: true, commitBehavior);            
        }

        private AsyncCompletionData.CommitBehavior Commit(
            Document document,
            CompletionService completionService,
            ITextView view,
            ITextBuffer subjectBuffer,
            RoslynCompletionItem roslynItem,
            char? commitCharacter,
            ITextSnapshot triggerSnapshot,
            CompletionRules rules,
            string filterText,
            CancellationToken cancellationToken)
        {
            bool includesCommitCharacter;
            if (!subjectBuffer.CheckEditAccess())
            {
                // We are on the wrong thread.
                FatalError.ReportWithoutCrash(new AccessViolationException("Subject buffer did not provide Edit Access"));
                return AsyncCompletionData.CommitBehavior.None;
            }

            if (subjectBuffer.EditInProgress)
            {
                FatalError.ReportWithoutCrash(new AccessViolationException("Subject buffer is editing by someone else."));
                return AsyncCompletionData.CommitBehavior.None;
            }

            using (var edit = subjectBuffer.CreateEdit())
            {
                var change = completionService.GetChangeAsync(document, roslynItem, commitCharacter, cancellationToken).WaitAndGetResult(cancellationToken);

                var textChange = change.TextChange;

                var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
                var mappedSpan = triggerSnapshotSpan.TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                edit.Replace(mappedSpan.Span, change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, change.NewPosition.Value));
                }

                includesCommitCharacter = change.IncludesCommitCharacter;

                if (roslynItem.Rules.FormatOnCommit)
                {
                    // refresh the document
                    document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    var spanToFormat = triggerSnapshotSpan.TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
                    var formattingService = document?.GetLanguageService<IEditorFormattingService>();

                    if (formattingService != null)
                    {
                        var changes = formattingService.GetFormattingChangesAsync(
                            document, spanToFormat.Span.ToTextSpan(), CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                        document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, CancellationToken.None);
                    }
                }
            }

            if (includesCommitCharacter)
            {
                return AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers;
            }

            if (commitCharacter == '\n' && Controller.SendEnterThroughToEditor(rules, roslynItem, filterText))
            {
                return AsyncCompletionData.CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers;
            }

            return AsyncCompletionData.CommitBehavior.None;
        }
    }
}
