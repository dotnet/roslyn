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

            var completionService = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionSource.RoslynItem, out var roslynItem))
            {
                // This isn't an item we provided (e.g. Razor). Let the editor handle it normally.
                return CommitResultUnhandled;
            }

            // We can be called before for ShouldCommitCompletion. However, that call does not provide rules applied for the completion item.
            // Now we check for the commit charcter in the context of Rules that could change the list of commit characters.
            if (!IsCommitCharacter(typeChar, roslynItem.Rules.CommitCharacterRules))
            {
                return new AsyncCompletionData.CommitResult(isHandled: true, AsyncCompletionData.CommitBehavior.CancelCommit);
            }

            if (completionService.GetProvider(roslynItem) is IFeaturesCustomCommitCompletionProvider featuresCustomCommitCompletionProvider)
            {
                // Custom commit provider assumes that null is provided is case of invoke. VS provides '\0' in the case.
                char? commitChar = typeChar == '\0' ? null : (char?)typeChar;
                var commitBehavior = CustomCommit(document, featuresCustomCommitCompletionProvider, session.TextView, 
                    subjectBuffer, roslynItem, session.ApplicableToSpan, commitChar, cancellationToken);

                return new AsyncCompletionData.CommitResult(isHandled: true, commitBehavior);
            }

            // TODO Remove language specific code: https://github.com/dotnet/roslyn/issues/30276
            if (document.Project.Language == LanguageNames.VisualBasic && typeChar == '\n')
            {
                return new AsyncCompletionData.CommitResult(isHandled: false, 
                    AsyncCompletionData.CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers);
            }

            if (item.InsertText.EndsWith(":") && typeChar == ':')
            {
                return new AsyncCompletionData.CommitResult(isHandled: false, 
                    AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
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

        private AsyncCompletionData.CommitBehavior CustomCommit(
            Document document,
            IFeaturesCustomCommitCompletionProvider provider,
            ITextView view,
            ITextBuffer subjectBuffer,
            RoslynCompletionItem roslynItem,
            ITrackingSpan applicableSpan,
            char? commitCharacter,
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
                var currentViewSpan = applicableSpan.GetSpan(applicableSpan.TextBuffer.CurrentSnapshot);
                var spans = view.BufferGraph.MapDownToBuffer(currentViewSpan, SpanTrackingMode.EdgeInclusive, subjectBuffer);

                // There can be no spans in case of projections but must be less than 2 spans.
                // 1 span is a regular case.
                // 0 spans means the a span is in anoter projection.
                // We do not expect more than 1 span, and do not support this case.
                System.Diagnostics.Debug.Assert(spans.Count < 2);
                if (spans.Any())
                {
                    var currentBufferSpan = spans[0].TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
                    edit.Delete(currentBufferSpan);
                }

                var change = provider.GetChangeAsync(document, roslynItem, commitCharacter, cancellationToken).WaitAndGetResult(cancellationToken);

                // Note that we use here a snapshot created in GetCompletionContextAsync. Be sure it is up-to-date.
                // TODO: add a telemetry event if delete and replace spans overloap: https://github.com/dotnet/roslyn/issues/30277
                edit.Replace(change.TextChange.Span.ToSpan(), change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, change.NewPosition.Value));
                }

                includesCommitCharacter = change.IncludesCommitCharacter;
            }

            return includesCommitCharacter
                ? AsyncCompletionData.CommitBehavior.SuppressFurtherTypeCharCommandHandlers 
                : AsyncCompletionData.CommitBehavior.None;
        }
    }
}
