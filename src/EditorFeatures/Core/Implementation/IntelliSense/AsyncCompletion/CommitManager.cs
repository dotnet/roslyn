// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
    internal sealed class CommitManager : IAsyncCompletionCommitManager
    {
        private static readonly IEnumerable<char> s_commitChars = ImmutableArray.Create(
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');

        private static readonly AsyncCompletionData.CommitResult CommitResultUnhandled =
            new AsyncCompletionData.CommitResult(isHandled: false, AsyncCompletionData.CommitBehavior.None);

        public IEnumerable<char> PotentialCommitCharacters => s_commitChars;

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
            => s_commitChars.Contains(typedChar);

        public AsyncCompletionData.CommitResult TryCommit(
            IAsyncCompletionSession session,
            ITextBuffer subjectBuffer,
            VSCompletionItem item,
            char typeChar,
            CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return CommitResultUnhandled;
            }

            if (!(document.GetLanguageService<CompletionService>() is CompletionServiceWithProviders completionService))
            {
                return CommitResultUnhandled;
            }

            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionSource.RoslynItem, out var roslynItem))
            {
                // This isn't an item we provided (e.g. Razor). Let the editor handle it normally.
                return CommitResultUnhandled;
            }

            var filterText = session.ApplicableToSpan.GetText(subjectBuffer.CurrentSnapshot);
            if (Controller.IsFilterCharacter(roslynItem, typeChar, filterText + typeChar))
            { 
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            // We can be called before for ShouldCommitCompletion. However, that call does not provide rules applied for the completion item.
            // Now we check for the commit charcter in the context of Rules that could change the list of commit characters.
            if (!IsCommitCharacter(typeChar, roslynItem.Rules.CommitCharacterRules))
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            var provider = completionService.GetProvider(roslynItem);
            if (provider != null && item.Properties.TryGetProperty<ITextSnapshot>(CompletionSource.TriggerSnapshot, out var triggerSnapshot))
            {
                // Custom commit provider assumes that null is provided is case of invoke. VS provides '\0' in the case.
                char? commitChar = typeChar == '\0' ? null : (char?)typeChar;
                var commitBehavior = Commit(document, provider, session.TextView, subjectBuffer, roslynItem, commitChar, triggerSnapshot, cancellationToken);

                return new AsyncCompletionData.CommitResult(isHandled: true, commitBehavior);
            }

            if (item.InsertText.EndsWith(":") && typeChar == ':')
            {
                return new AsyncCompletionData.CommitResult(isHandled: false, AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
            }

            return CommitResultUnhandled;
        }

        /// <summary>
        /// This method needs to support custom procesing of commit characters to be on par with the old completion implementation.
        /// </summary>
        private bool IsCommitCharacter(char typeChar, ImmutableArray<CharacterSetModificationRule> rules)
        {
            // Tab, Enter and Null (call invoke commit) are always a commit character
            if (typeChar == '\t' || typeChar == '\n' || typeChar == '\0')
            {
                return true;
            }

            foreach (var rule in rules)
            {
                switch (rule.Kind)
                {
                    case CharacterSetModificationKind.Add:
                        if (rule.Characters.Contains(typeChar))
                        {
                            return true;
                        }

                        break;

                    case CharacterSetModificationKind.Remove:
                        if (rule.Characters.Contains(typeChar))
                        {
                            return false;
                        }

                        break;

                    case CharacterSetModificationKind.Replace:
                        return rule.Characters.Contains(typeChar);
                }
            }

            return s_commitChars.Contains(typeChar);
        }

        private AsyncCompletionData.CommitBehavior Commit(
            Document document,
            CompletionProvider provider,
            ITextView view,
            ITextBuffer subjectBuffer,
            RoslynCompletionItem roslynItem,
            char? commitCharacter,
            ITextSnapshot triggerSnapshot,
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
                var change = provider.GetChangeAsync(document, roslynItem, commitCharacter, cancellationToken).WaitAndGetResult(cancellationToken);

                var textChange = change.TextChange;

                var triggerSnapshotSpan = new SnapshotSpan(triggerSnapshot, textChange.Span.ToSpan());
                var mappedSpan = triggerSnapshotSpan.TranslateTo(
                    subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                edit.Replace(mappedSpan.Span, change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, change.NewPosition.Value));
                }

                includesCommitCharacter = change.IncludesCommitCharacter;
            }

            if (includesCommitCharacter) return AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers;

            // TODO Remove language specific code: https://github.com/dotnet/roslyn/issues/30276
            if (commitCharacter == '\n' && document.Project.Language == LanguageNames.VisualBasic) return AsyncCompletionData.CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers;
            return AsyncCompletionData.CommitBehavior.None;
        }
    }
}
