// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed class CommitManager : ForegroundThreadAffinitizedObject, IAsyncCompletionCommitManager
    {
        private static readonly AsyncCompletionData.CommitResult CommitResultUnhandled =
            new(isHandled: false, AsyncCompletionData.CommitBehavior.None);

        private readonly RecentItemsManager _recentItemsManager;
        private readonly ITextView _textView;
        private readonly IGlobalOptionService _globalOptions;

        public IEnumerable<char> PotentialCommitCharacters
        {
            get
            {
                if (_textView.Properties.TryGetProperty(CompletionSource.PotentialCommitCharacters, out ImmutableArray<char> potentialCommitCharacters))
                {
                    return potentialCommitCharacters;
                }
                else
                {
                    // If we were not initialized with a CompletionService or are called for a wrong textView, we should not make a commit.
                    return ImmutableArray<char>.Empty;
                }
            }
        }

        internal CommitManager(ITextView textView, RecentItemsManager recentItemsManager, IGlobalOptionService globalOptions, IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _globalOptions = globalOptions;
            _recentItemsManager = recentItemsManager;
            _textView = textView;
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

            if (!CompletionItemData.TryGetData(item, out var itemData))
            {
                // Roslyn should not be called if the item committing was not provided by Roslyn.
                return CommitResultUnhandled;
            }

            var filterText = session.ApplicableToSpan.GetText(session.ApplicableToSpan.TextBuffer.CurrentSnapshot) + typeChar;
            if (Helpers.IsFilterCharacter(itemData.RoslynItem, typeChar, filterText))
            {
                // Returning Cancel means we keep the current session and consider the character for further filtering.
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            var options = _globalOptions.GetCompletionOptions(document.Project.Language);
            var serviceRules = completionService.GetRules(options);

            // We can be called before for ShouldCommitCompletion. However, that call does not provide rules applied for the completion item.
            // Now we check for the commit character in the context of Rules that could change the list of commit characters.

            if (!Helpers.IsStandardCommitCharacter(typeChar) && !IsCommitCharacter(serviceRules, itemData.RoslynItem, typeChar))
            {
                // Returning None means we complete the current session with a void commit. 
                // The Editor then will try to trigger a new completion session for the character.
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.None);
            }

            if (!itemData.TriggerLocation.HasValue)
            {
                // Need the trigger snapshot to calculate the span when the commit changes to be applied.
                // They should always be available from items provided by Roslyn CompletionSource.
                // Just to be defensive, if it's not found here, Roslyn should not make a commit.
                return CommitResultUnhandled;
            }

            var triggerDocument = itemData.TriggerLocation.Value.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (triggerDocument == null)
            {
                return CommitResultUnhandled;
            }

            var sessionData = CompletionSessionData.GetOrCreateSessionData(session);
            if (!sessionData.CompletionListSpan.HasValue)
            {
                return CommitResultUnhandled;
            }

            // Telemetry
            if (sessionData.TargetTypeFilterExperimentEnabled)
            {
                // Capture the % of committed completion items that would have appeared in the "Target type matches" filter
                // (regardless of whether that filter button was active at the time of commit).
                AsyncCompletionLogger.LogCommitWithTargetTypeCompletionExperimentEnabled();
                if (item.Filters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches))
                {
                    AsyncCompletionLogger.LogCommitItemWithTargetTypeFilter();
                }
            }

            // Commit with completion service assumes that null is provided is case of invoke. VS provides '\0' in the case.
            var commitChar = typeChar == '\0' ? null : (char?)typeChar;
            return Commit(
                session, triggerDocument, completionService, subjectBuffer,
                itemData.RoslynItem, sessionData.CompletionListSpan.Value, commitChar, itemData.TriggerLocation.Value.Snapshot, serviceRules,
                filterText, cancellationToken);
        }

        private AsyncCompletionData.CommitResult Commit(
            IAsyncCompletionSession session,
            Document document,
            CompletionService completionService,
            ITextBuffer subjectBuffer,
            RoslynCompletionItem roslynItem,
            TextSpan completionListSpan,
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
                FatalError.ReportAndCatch(new InvalidOperationException("Subject buffer did not provide Edit Access"), ErrorSeverity.Critical);
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.None);
            }

            if (subjectBuffer.EditInProgress)
            {
                FatalError.ReportAndCatch(new InvalidOperationException("Subject buffer is editing by someone else."), ErrorSeverity.Critical);
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.None);
            }

            CompletionChange change;

            // We met an issue when external code threw an OperationCanceledException and the cancellationToken is not canceled.
            // Catching this scenario for further investigations.
            // See https://github.com/dotnet/roslyn/issues/38455.
            try
            {
                // Cached items have a span computed at the point they were created.  This span may no 
                // longer be valid when used again.  In that case, override the span with the latest span
                // for the completion list itself.
                if (roslynItem.Flags.IsCached())
                    roslynItem.Span = completionListSpan;

                change = completionService.GetChangeAsync(document, roslynItem, commitCharacter, cancellationToken).WaitAndGetResult(cancellationToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken != cancellationToken && FatalError.ReportAndCatch(e))
            {
                return CommitResultUnhandled;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var view = session.TextView;

            var provider = GetCompletionProvider(completionService, roslynItem);
            if (provider is ICustomCommitCompletionProvider customCommitProvider)
            {
                customCommitProvider.Commit(roslynItem, view, subjectBuffer, triggerSnapshot, commitCharacter);
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.None);
            }

            var textChange = change.TextChange;
            var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
            var mappedSpan = triggerSnapshotSpan.TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

            using (var edit = subjectBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(mappedSpan.Span, change.TextChange.NewText);

                // edit.Apply() may trigger changes made by extensions.
                // updatedCurrentSnapshot will contain changes made by Roslyn but not by other extensions.
                var updatedCurrentSnapshot = edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    // Roslyn knows how to position the caret in the snapshot we just created.
                    // If there were more edits made by extensions, TryMoveCaretToAndEnsureVisible maps the snapshot point to the most recent one.
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(updatedCurrentSnapshot, change.NewPosition.Value));
                }
                else
                {
                    // Or, If we're doing a minimal change, then the edit that we make to the 
                    // buffer may not make the total text change that places the caret where we 
                    // would expect it to go based on the requested change. In this case, 
                    // determine where the item should go and set the care manually.

                    // Note: we only want to move the caret if the caret would have been moved 
                    // by the edit.  i.e. if the caret was actually in the mapped span that 
                    // we're replacing.
                    var caretPositionInBuffer = view.GetCaretPoint(subjectBuffer);
                    if (caretPositionInBuffer.HasValue && mappedSpan.IntersectsWith(caretPositionInBuffer.Value))
                    {
                        view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, mappedSpan.Start.Position + textChange.NewText?.Length ?? 0));
                    }
                    else
                    {
                        view.Caret.EnsureVisible();
                    }
                }

                includesCommitCharacter = change.IncludesCommitCharacter;

                if (roslynItem.Rules.FormatOnCommit)
                {
                    // The edit updates the snapshot however other extensions may make changes there.
                    // Therefore, it is required to use subjectBuffer.CurrentSnapshot for further calculations rather than the updated current snapshot defined above.
                    var currentDocument = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    var formattingService = currentDocument?.GetRequiredLanguageService<IFormattingInteractionService>();

                    if (currentDocument != null && formattingService != null)
                    {
                        var spanToFormat = triggerSnapshotSpan.TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
                        var changes = formattingService.GetFormattingChangesAsync(
                            currentDocument, spanToFormat.Span.ToTextSpan(), documentOptions: null, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                        currentDocument.Project.Solution.Workspace.ApplyTextChanges(currentDocument.Id, changes, CancellationToken.None);
                    }
                }
            }

            _recentItemsManager.MakeMostRecentItem(roslynItem.FilterText);

            if (provider is INotifyCommittingItemCompletionProvider notifyProvider)
            {
                _ = ThreadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    // Make sure the notification isn't sent on UI thread.
                    await TaskScheduler.Default;
                    _ = notifyProvider.NotifyCommittingItemAsync(document, roslynItem, commitCharacter, cancellationToken).ReportNonFatalErrorAsync();
                });
            }

            if (includesCommitCharacter)
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
            }

            if (commitCharacter == '\n' && SendEnterThroughToEditor(rules, roslynItem, filterText))
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers);
            }

            return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.None);
        }

        internal static bool IsCommitCharacter(CompletionRules completionRules, CompletionItem item, char ch)
        {
            // First see if the item has any specific commit rules it wants followed.
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

            // Fall back to the default rules for this language's completion service.
            return completionRules.DefaultCommitCharacters.IndexOf(ch) >= 0;
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
                    // textTypedSoFar is concatenated from individual chars typed.
                    // '\n' is the enter char.
                    // That is why, there is no need to check for '\r\n'.
                    if (textTypedSoFar.LastOrDefault() == '\n')
                    {
                        textTypedSoFar = textTypedSoFar.Substring(0, textTypedSoFar.Length - 1);
                    }

                    return item.GetEntireDisplayText() == textTypedSoFar;
            }
        }

        private static CompletionProvider? GetCompletionProvider(CompletionService completionService, CompletionItem item)
        {
            if (completionService is CompletionServiceWithProviders completionServiceWithProviders)
            {
                return completionServiceWithProviders.GetProvider(item);
            }

            return null;
        }
    }
}
