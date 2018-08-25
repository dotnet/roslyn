// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract partial class CommonCompletionService : CompletionServiceWithProviders
    {
        protected CommonCompletionService(
            Workspace workspace,
            ImmutableArray<CompletionProvider>? exclusiveProviders)
            : base(workspace, exclusiveProviders)
        {
        }

        protected override CompletionItem GetBetterItem(CompletionItem item, CompletionItem existingItem)
        {
            // We've constructed the export order of completion providers so 
            // that snippets are exported after everything else. That way,
            // when we choose a single item per display text, snippet 
            // glyphs appear by snippets. This breaks preselection of items
            // whose display text is also a snippet (workitem 852578),
            // the snippet item doesn't have its preselect bit set.
            // We'll special case this by not preferring later items
            // if they are snippets and the other candidate is preselected.
            if (existingItem.Rules.MatchPriority != MatchPriority.Default && IsSnippetItem(item))
            {
                return existingItem;
            }

            return base.GetBetterItem(item, existingItem);
        }

        protected static bool IsKeywordItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Keyword);
        }

        protected static bool IsSnippetItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Snippet);
        }
    }

    internal class Dispatcher : CompletionService, ILanguageService
    {
        private readonly Workspace _workspace;
        private readonly CompletionService _delegatee;

        public Dispatcher(Workspace workspace, CompletionService delegatee)
        {
            _workspace = workspace;
            _delegatee = delegatee;
        }

        // we don't care about third party provider for now
        public override string Language => _delegatee.Language;
        public override CompletionRules GetRules() => _delegatee.GetRules();
        public override TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition) => _delegatee.GetDefaultCompletionListSpan(text, caretPosition);

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            // for now, run this in proc, since these are synchronous API. 
            // but we should make these async and run on OOP to get more realistic
            return _delegatee.ShouldTriggerCompletion(text, caretPosition, trigger, roles, options);
        }

        public override ImmutableArray<CompletionItem> FilterItems(Document document, ImmutableArray<CompletionItem> items, string filterText)
        {
            // for now, run this in proc, since these are synchronous API
            // but we should make these async and run on OOP to get more realistic
            //
            // this also require whole items to move between 2 processes. this is fine when things are in same process, but just too expensive to do between
            // processes. this design need to be changed if we want to run completion OOP.
            return _delegatee.FilterItems(document, items, filterText);
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitCharacter = null, CancellationToken cancellationToken = default)
        {
            var client = await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await _delegatee.GetChangeAsync(document, item, commitCharacter, cancellationToken).ConfigureAwait(false);
            }

            return await client.TryRunCodeAnalysisRemoteAsync<CompletionChange>(
                document.Project.Solution, "CompletionGetChangeAsync", new object[] { document.Id, item, item.Document?.Id, commitCharacter.HasValue ? commitCharacter.Value : default }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken = default)
        {
            var client = await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await _delegatee.GetDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false);
            }

            return await client.TryRunCodeAnalysisRemoteAsync<CompletionDescription>(
                document.Project.Solution, "CompletionGetDescriptionAsync", new object[] { document.Id, item, item.Document?.Id }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<CompletionList> GetCompletionsAsync(Document document, int caretPosition, CompletionTrigger trigger = default, ImmutableHashSet<string> roles = null, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var client = await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await _delegatee.GetCompletionsAsync(document, caretPosition, trigger, roles, options, cancellationToken).ConfigureAwait(false);
            }

            // for prototype, ignore options
            var result = await client.TryRunCodeAnalysisRemoteAsync<CompletionListResult>(
                document.Project.Solution, "CompletionGetCompletionsAsync", new object[] { document.Id, caretPosition, trigger, (ISet<string>)roles }, cancellationToken).ConfigureAwait(false);

            if (result.CompletionList == null)
            {
                return null;
            }

            for (var i = 0; i < result.TriggerDocumentId.Count; i++)
            {
                // it is wierd that completion list document is mutable.. though
                result.CompletionList.Items[i].Document = document.Project.Solution.GetDocument(result.TriggerDocumentId[i]);
            }

            if (result.CompletionList?.SuggestionModeItem != null)
            {
                result.CompletionList.SuggestionModeItem.Document = document.Project.Solution.GetDocument(result.SuggestionModeItemTriggerDocumentId);
            }

            return result.CompletionList;
        }
    }

    internal class CompletionListResult
    {
        public IList<DocumentId> TriggerDocumentId { get; set; }
        public DocumentId SuggestionModeItemTriggerDocumentId { get; set; }
        public CompletionList CompletionList { get; set; }
    }
}
