// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    public abstract partial class CompletionService
    {
        /// <summary>
        /// Gets the completions available at the caret position.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <param name="cancellationToken"></param>
        public Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger = default,
            ImmutableHashSet<string>? roles = null,
            OptionSet? options = null,
            CancellationToken cancellationToken = default)
        {
            // Publicly available options do not affect this API. Force complete results from this public API since
            // external consumers do not have access to Roslyn's waiters.
            var completionOptions = CompletionOptions.Default with { ForceExpandedCompletionIndexCreation = true };
            var passThroughOptions = options ?? document.Project.Solution.Options;

            return GetCompletionsAsync(document, caretPosition, completionOptions, passThroughOptions, trigger, roles, cancellationToken);
        }

        /// <summary>
        /// Gets the completions available at the caret position.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="options">The CompletionOptions that override the default options.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="cancellationToken"></param>
        internal virtual async Task<CompletionList> GetCompletionsAsync(
             Document document,
             int caretPosition,
             CompletionOptions options,
             OptionSet passThroughOptions,
             CompletionTrigger trigger = default,
             ImmutableHashSet<string>? roles = null,
             CancellationToken cancellationToken = default)
        {
            // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
            (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var completionListSpan = GetDefaultCompletionListSpan(text, caretPosition);

            var providers = _providerManager.GetFilteredProviders(document.Project, roles, trigger, options);

            // Phase 1: Completion Providers decide if they are triggered based on textual analysis
            // Phase 2: Completion Providers use syntax to confirm they are triggered, or decide they are not actually triggered and should become an augmenting provider
            // Phase 3: Triggered Providers are asked for items
            // Phase 4: If any items were provided, all augmenting providers are asked for items
            // This allows a provider to be textually triggered but later decide to be an augmenting provider based on deeper syntactic analysis.

            var triggeredProviders = GetTriggeredProviders(document, providers, caretPosition, options, trigger, roles, text);

            var additionalAugmentingProviders = await GetAugmentingProvidersAsync(document, triggeredProviders, caretPosition, trigger, options, cancellationToken).ConfigureAwait(false);
            triggeredProviders = triggeredProviders.Except(additionalAugmentingProviders).ToImmutableArray();

            // PERF: Many CompletionProviders compute identical contexts. This actually shows up on the 2-core typing test.
            // so we try to share a single SyntaxContext based on document/caretPosition among all providers to reduce repeat computation.
            var sharedContext = new SharedSyntaxContextsWithSpeculativeModel(document, caretPosition);

            // Now, ask all the triggered providers, in parallel, to populate a completion context.
            // Note: we keep any context with items *or* with a suggested item.  
            var triggeredContexts = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options, completionListSpan, triggeredProviders, sharedContext, cancellationToken).ConfigureAwait(false);

            // Nothing to do if we didn't even get any regular items back (i.e. 0 items or suggestion item only.)
            if (!triggeredContexts.Any(static cc => cc.Items.Count > 0))
                return CompletionList.Empty;

            // See if there were completion contexts provided that were exclusive. If so, then
            // that's all we'll return.
            var exclusiveContexts = triggeredContexts.Where(t => t.IsExclusive).ToImmutableArray();
            if (!exclusiveContexts.IsEmpty)
                return MergeAndPruneCompletionLists(exclusiveContexts, options, isExclusive: true);

            // Great!  We had some items.  Now we want to see if any of the other providers 
            // would like to augment the completion list.  For example, we might trigger
            // enum-completion on space.  If enum completion results in any items, then 
            // we'll want to augment the list with all the regular symbol completion items.
            var augmentingProviders = providers.Except(triggeredProviders).ToImmutableArray();

            var augmentingContexts = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options, completionListSpan, augmentingProviders, sharedContext, cancellationToken).ConfigureAwait(false);

            GC.KeepAlive(semanticModel);

            // Providers are ordered, but we processed them in our own order.  Ensure that the
            // groups are properly ordered based on the original providers.
            var completionProviderToIndex = GetCompletionProviderToIndex(providers);
            var allContexts = triggeredContexts.Concat(augmentingContexts)
                .Sort((p1, p2) => completionProviderToIndex[p1.Provider] - completionProviderToIndex[p2.Provider]);

            return MergeAndPruneCompletionLists(allContexts, options, isExclusive: false);

            ImmutableArray<CompletionProvider> GetTriggeredProviders(
                Document document, ConcatImmutableArray<CompletionProvider> providers, int caretPosition, CompletionOptions options, CompletionTrigger trigger, ImmutableHashSet<string>? roles, SourceText text)
            {
                switch (trigger.Kind)
                {
                    case CompletionTriggerKind.Insertion:
                    case CompletionTriggerKind.Deletion:

                        if (ShouldTriggerCompletion(document.Project, document.Project.Services, text, caretPosition, trigger, options, passThroughOptions, roles))
                        {
                            var triggeredProviders = providers.Where(p => p.ShouldTriggerCompletion(document.Project.Services, text, caretPosition, trigger, options, passThroughOptions)).ToImmutableArrayOrEmpty();

                            Debug.Assert(ValidatePossibleTriggerCharacterSet(trigger.Kind, triggeredProviders, document, text, caretPosition, options));
                            return triggeredProviders.IsEmpty ? providers.ToImmutableArray() : triggeredProviders;
                        }

                        return ImmutableArray<CompletionProvider>.Empty;

                    default:
                        return providers.ToImmutableArray();
                }
            }

            static async Task<ImmutableArray<CompletionProvider>> GetAugmentingProvidersAsync(
                Document document, ImmutableArray<CompletionProvider> triggeredProviders, int caretPosition, CompletionTrigger trigger, CompletionOptions options, CancellationToken cancellationToken)
            {
                var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
                var additionalAugmentingProviders = ArrayBuilder<CompletionProvider>.GetInstance(triggeredProviders.Length);
                if (trigger.Kind == CompletionTriggerKind.Insertion)
                {
                    foreach (var provider in triggeredProviders)
                    {
                        var isSyntacticTrigger = await extensionManager.PerformFunctionAsync(
                            provider,
                            () => provider.IsSyntacticTriggerCharacterAsync(document, caretPosition, trigger, options, cancellationToken),
                            defaultValue: false).ConfigureAwait(false);
                        if (!isSyntacticTrigger)
                            additionalAugmentingProviders.Add(provider);
                    }
                }

                return additionalAugmentingProviders.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Returns a document with frozen partial semantic unless we already have a complete compilation available.
        /// Getting full semantic could be costly in certain scenarios and would cause significant delay in completion. 
        /// In most cases we'd still end up with complete document, but we'd consider it an acceptable trade-off even when 
        /// we get into this transient state.
        /// </summary>
        private async Task<(Document document, SemanticModel? semanticModel)> GetDocumentWithFrozenPartialSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (_suppressPartialSemantics)
            {
                return (document, await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false));
            }

            return await document.GetFullOrPartialSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        }

        private static bool ValidatePossibleTriggerCharacterSet(CompletionTriggerKind completionTriggerKind, IEnumerable<CompletionProvider> triggeredProviders,
            Document document, SourceText text, int caretPosition, in CompletionOptions options)
        {
            // Only validate on insertion triggers.
            if (completionTriggerKind != CompletionTriggerKind.Insertion)
            {
                return true;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (caretPosition > 0 && syntaxFactsService != null)
            {
                // The trigger character has already been inserted before the current caret position.
                var character = text[caretPosition - 1];

                // Identifier characters are not part of the possible trigger character set, so don't validate them.
                var isIdentifierCharacter = syntaxFactsService.IsIdentifierStartCharacter(character) || syntaxFactsService.IsIdentifierEscapeCharacter(character);
                if (isIdentifierCharacter)
                {
                    return true;
                }

                // Only verify against built in providers.  3rd party ones do not necessarily implement the possible trigger characters API.
                foreach (var provider in triggeredProviders)
                {
                    if (provider is LSPCompletionProvider lspProvider && lspProvider.IsInsertionTrigger(text, caretPosition - 1, options))
                    {
                        if (!lspProvider.TriggerCharacters.Contains(character))
                        {
                            Debug.Assert(lspProvider.TriggerCharacters.Contains(character),
                            $"the character {character} is not a valid trigger character for {lspProvider.Name}");
                        }
                    }
                }
            }

            return true;
        }

        private static bool HasAnyItems(CompletionContext cc)
            => cc.Items.Count > 0 || cc.SuggestionModeItem != null;

        private static async Task<ImmutableArray<CompletionContext>> ComputeNonEmptyCompletionContextsAsync(
            Document document, int caretPosition, CompletionTrigger trigger,
            CompletionOptions options, TextSpan completionListSpan,
            ImmutableArray<CompletionProvider> providers,
            SharedSyntaxContextsWithSpeculativeModel sharedContext,
            CancellationToken cancellationToken)
        {
            var completionContextTasks = new List<Task<CompletionContext>>();
            foreach (var provider in providers)
            {
                completionContextTasks.Add(GetContextAsync(
                    provider, document, caretPosition, trigger,
                    options, completionListSpan, sharedContext, cancellationToken));
            }

            var completionContexts = await Task.WhenAll(completionContextTasks).ConfigureAwait(false);
            return completionContexts.Where(HasAnyItems).ToImmutableArray();
        }

        private CompletionList MergeAndPruneCompletionLists(
            ImmutableArray<CompletionContext> completionContexts,
            in CompletionOptions options,
            bool isExclusive)
        {
            Debug.Assert(!completionContexts.IsDefaultOrEmpty);

            using var displayNameToItemsMap = new DisplayNameToItemsMap(this);
            CompletionItem? suggestionModeItem = null;

            foreach (var context in completionContexts)
            {
                foreach (var item in context.Items)
                {
                    Debug.Assert(item != null);
                    displayNameToItemsMap.Add(item);
                }

                // first one wins
                suggestionModeItem ??= context.SuggestionModeItem;
            }

            if (displayNameToItemsMap.IsEmpty)
            {
                return CompletionList.Empty;
            }

            return CompletionList.Create(
                completionContexts[0].CompletionListSpan,   // All contexts have the same completion list span.
                displayNameToItemsMap.ToSegmentedList(options),
                GetRules(options),
                suggestionModeItem,
                isExclusive);
        }

        /// <summary>
        /// Determines if the items are similar enough they should be represented by a single item in the list.
        /// </summary>
        protected virtual bool ItemsMatch(CompletionItem item, CompletionItem existingItem)
        {
            return item.Span == existingItem.Span
                && item.SortText == existingItem.SortText && item.InlineDescription == existingItem.InlineDescription;
        }

        /// <summary>
        /// Determines which of two items should represent the matching pair.
        /// </summary>
        protected virtual CompletionItem GetBetterItem(CompletionItem item, CompletionItem existingItem)
        {
            // the item later in the sort order (determined by provider order) wins?
            return item;
        }

        private static Dictionary<CompletionProvider, int> GetCompletionProviderToIndex(ConcatImmutableArray<CompletionProvider> completionProviders)
        {
            var result = new Dictionary<CompletionProvider, int>(completionProviders.Length);

            var i = 0;
            foreach (var completionProvider in completionProviders)
            {
                result[completionProvider] = i;
                i++;
            }

            return result;
        }

        private static async Task<CompletionContext> GetContextAsync(
            CompletionProvider provider,
            Document document,
            int position,
            CompletionTrigger triggerInfo,
            CompletionOptions options,
            TextSpan defaultSpan,
            SharedSyntaxContextsWithSpeculativeModel? sharedContext,
            CancellationToken cancellationToken)
        {
            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

            var context = new CompletionContext(provider, document, position, sharedContext, defaultSpan, triggerInfo, options, cancellationToken);

            // Wrap with extension manager call.  This will ensure this provider is not disabled.  If not, it will ask
            // it for completions.  If that throws, then the provider will be moved to the disabled state.
            await extensionManager.PerformActionAsync(
                provider,
                () => provider.ProvideCompletionsAsync(context)).ConfigureAwait(false);

            return context;
        }

        private class DisplayNameToItemsMap(CompletionService service) : IEnumerable<CompletionItem>, IDisposable
        {
            // We might need to handle large amount of items with import completion enabled,
            // so use a dedicated pool to minimize array allocations. Set the size of pool to a small
            // number 5 because we don't expect more than a couple of callers at the same time.
            private static readonly ObjectPool<Dictionary<string, object>> s_uniqueSourcesPool = new(factory: () => new Dictionary<string, object>(), size: 5);
            private static readonly ObjectPool<List<CompletionItem>> s_sortListPool = new(factory: () => new List<CompletionItem>(), size: 5);

            private readonly Dictionary<string, object> _displayNameToItemsMap = s_uniqueSourcesPool.Allocate();
            private readonly CompletionService _service = service;

            public int Count { get; private set; }

            public SegmentedList<CompletionItem> ToSegmentedList(in CompletionOptions options)
            {
                if (!options.PerformSort)
                {
                    return new(this);
                }

                // Use a list to do the sorting as it's significantly faster than doing so on a SegmentedList.
                var list = s_sortListPool.Allocate();
                try
                {
                    list.AddRange(this);
                    list.Sort();
                    return new(list);
                }
                finally
                {
                    list.Clear();
                    s_sortListPool.Free(list);
                }
            }

            public void Dispose()
            {
                _displayNameToItemsMap.Clear();
                s_uniqueSourcesPool.Free(_displayNameToItemsMap);
            }

            public bool IsEmpty => _displayNameToItemsMap.Count == 0;

            public void Add(CompletionItem item)
            {
                var entireDisplayText = item.GetEntireDisplayText();

                if (!_displayNameToItemsMap.TryGetValue(entireDisplayText, out var value))
                {
                    Count++;
                    _displayNameToItemsMap.Add(entireDisplayText, item);
                    return;
                }

                // If two items have the same display text choose which one to keep.
                // If they don't actually match keep both.
                if (value is CompletionItem sameNamedItem)
                {
                    if (_service.ItemsMatch(item, sameNamedItem))
                    {
                        _displayNameToItemsMap[entireDisplayText] = _service.GetBetterItem(item, sameNamedItem);
                        return;
                    }

                    Count++;
                    // Matching items should be rare, no need to use object pool for this.
                    _displayNameToItemsMap[entireDisplayText] = new List<CompletionItem>() { sameNamedItem, item };
                }
                else if (value is List<CompletionItem> sameNamedItems)
                {
                    for (var i = 0; i < sameNamedItems.Count; i++)
                    {
                        var existingItem = sameNamedItems[i];
                        if (_service.ItemsMatch(item, existingItem))
                        {
                            sameNamedItems[i] = _service.GetBetterItem(item, existingItem);
                            return;
                        }
                    }

                    Count++;
                    sameNamedItems.Add(item);
                }
            }

            public IEnumerator<CompletionItem> GetEnumerator()
            {
                foreach (var value in _displayNameToItemsMap.Values)
                {
                    if (value is CompletionItem sameNamedItem)
                    {
                        yield return sameNamedItem;
                    }
                    else if (value is List<CompletionItem> sameNamedItems)
                    {
                        foreach (var item in sameNamedItems)
                        {
                            yield return item;
                        }
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
