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

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed class CommitManager : ForegroundThreadAffinitizedObject, IAsyncCompletionCommitManager
    {
        private static readonly AsyncCompletionData.CommitResult CommitResultUnhandled =
            new AsyncCompletionData.CommitResult(isHandled: false, AsyncCompletionData.CommitBehavior.None);

        private readonly RecentItemsManager _recentItemsManager;

        public IEnumerable<char> PotentialCommitCharacters { get; }

        internal CommitManager(ImmutableArray<char> potentialCommitCharacters, RecentItemsManager recentItemsManager, IThreadingContext threadingContext) : base(threadingContext)
        {
            PotentialCommitCharacters = potentialCommitCharacters;
            _recentItemsManager = recentItemsManager;
        }

        /// <summary>
        /// The method performs a preliminarily filtering of commit availability.
        /// In case of a doubt, it should respond with true.
        /// We will be able to cancel later in 
        /// <see cref="TryCommit(IAsyncCompletionSession, ITextBuffer, VSCompletionItem, char, CancellationToken)"/> 
        /// based on <see cref="VSCompletionItem"/> item, e.g. based on <see cref="CompletionItemRules"/>.
        /// </summary>
        public bool ShouldCommitCompletion(
            IAsyncCompletionSession session,
            SnapshotPoint location,
            char typedChar,
            CancellationToken cancellationToken)
        {
            if (!PotentialCommitCharacters.Contains(typedChar))
            {
                return false;
            }

            return !(session.Properties.TryGetProperty(CompletionSource.ExcludedCommitCharacters, out ImmutableArray<char> excludedCommitCharacter)
                && excludedCommitCharacter.Contains(typedChar));
        }

        public AsyncCompletionData.CommitResult TryCommit(
            IAsyncCompletionSession session,
            ITextBuffer subjectBuffer,
            VSCompletionItem item,
            char typeChar,
            CancellationToken cancellationToken)
        {
            // We can make changes to buffers. We would like to be sure nobody can change them at the same time.
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

            if (!item.Properties.TryGetProperty(CompletionSource.RoslynItem, out RoslynCompletionItem roslynItem))
            {
                // Roslyn should not be called if the item committing was not provided by Roslyn.
                return CommitResultUnhandled;
            }

            var filterText = session.ApplicableToSpan.GetText(session.ApplicableToSpan.TextBuffer.CurrentSnapshot) + typeChar;
            if (Helpers.IsFilterCharacter(roslynItem, typeChar, filterText))
            { 
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            var serviceRules = completionService.GetRules();

            // We can be called before for ShouldCommitCompletion. However, that call does not provide rules applied for the completion item.
            // Now we check for the commit charcter in the context of Rules that could change the list of commit characters.

            // Tab, Enter and Null (call invoke commit) are always commit characters. 
            if (typeChar != '\t' && typeChar != '\n' && typeChar != '\0' && !IsCommitCharacter(serviceRules, roslynItem, typeChar, filterText))
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            if (!session.Properties.TryGetProperty(CompletionSource.TriggerSnapshot, out ITextSnapshot triggerSnapshot))
            {
                // Need the trigger snapshot to calculate the span when the commit changes to be applied.
                // It should be inserted into a property bag within GetCompletionContextAsync for each item created by Roslyn.
                // If not found here, Roslyn should not make a commit.
                return CommitResultUnhandled;
            }

            // Commit with completion service assumes that null is provided is case of invoke. VS provides '\0' in the case.
            char? commitChar = typeChar == '\0' ? null : (char?)typeChar;
            var commitBehavior = Commit(
                document, completionService, session.TextView, subjectBuffer, 
                roslynItem, commitChar, triggerSnapshot, serviceRules, filterText, cancellationToken);

            _recentItemsManager.MakeMostRecentItem(roslynItem.DisplayText);
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
            AssertIsForeground();

            bool includesCommitCharacter;
            if (!subjectBuffer.CheckEditAccess())
            {
                // We are on the wrong thread.
                FatalError.ReportWithoutCrash(new InvalidOperationException("Subject buffer did not provide Edit Access"));
                return AsyncCompletionData.CommitBehavior.None;
            }

            if (subjectBuffer.EditInProgress)
            {
                FatalError.ReportWithoutCrash(new InvalidOperationException("Subject buffer is editing by someone else."));
                return AsyncCompletionData.CommitBehavior.None;
            }

            using (var edit = subjectBuffer.CreateEdit())
            {
                var change = completionService.GetChangeAsync(document, roslynItem, commitCharacter, cancellationToken).WaitAndGetResult(cancellationToken);

                var textChange = change.TextChange;

                var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
                var mappedSpan = triggerSnapshotSpan.TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                edit.Replace(mappedSpan.Span, change.TextChange.NewText);

                // The edit updates the snapshot however other extensions may make changes there.
                // Therefore, it is safe to consume subjectBuffer.CurrentSnapshot for further calculations.
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

            if (commitCharacter == '\n' && SendEnterThroughToEditor(rules, roslynItem, filterText))
            {
                return AsyncCompletionData.CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers;
            }

            return AsyncCompletionData.CommitBehavior.None;
        }

        internal static bool IsCommitCharacter(CompletionRules completionRules, CompletionItem item, char ch, string textTypedSoFar)
        {
            // First see if the item has any specifc commit rules it wants followed.
            foreach (var rule in item.Rules.CommitCharacterRules)
            {
                switch (rule.Kind)
                {
                    case CharacterSetModificationKind.Add:
                        if (rule.Characters.Contains(ch))
                        {
                            return true;
                        }
                        continue;

                    case CharacterSetModificationKind.Remove:
                        if (rule.Characters.Contains(ch))
                        {
                            return false;
                        }
                        continue;

                    case CharacterSetModificationKind.Replace:
                        return rule.Characters.Contains(ch);
                }
            }

            // general rule: if the filtering text exactly matches the start of the item then it must be a filter character
            if (TextTypedSoFarMatchesItem(item, textTypedSoFar))
            {
                return false;
            }

            // Fall back to the default rules for this language's completion service.
            return completionRules.DefaultCommitCharacters.IndexOf(ch) >= 0;
        }

        internal static bool TextTypedSoFarMatchesItem(CompletionItem item, string textTypedSoFar)
        {
            if (textTypedSoFar.Length > 0)
            {
                return item.DisplayText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase) ||
                       item.FilterText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase);
            }

            return false;
        }

        internal static bool SendEnterThroughToEditor(CompletionRules rules, RoslynCompletionItem item, string textTypedSoFar)
        {
            var rule = item.Rules.EnterKeyRule;
            if (rule == EnterKeyRule.Default)
            {
                rule = rules.DefaultEnterKeyRule;
            }

            switch (rule)
            {
                default:
                case EnterKeyRule.Default:
                case EnterKeyRule.Never:
                    return false;
                case EnterKeyRule.Always:
                    return true;
                case EnterKeyRule.AfterFullyTypedWord:
                    return item.DisplayText + item.DisplayTextSuffix == textTypedSoFar;
            }
        }
    }
}
