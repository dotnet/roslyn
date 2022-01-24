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
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A subtype of <see cref="CompletionService"/> that aggregates completions from one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    public abstract partial class CompletionServiceWithProviders : CompletionService
    {
        private readonly Workspace _workspace;
        private readonly ProviderManager _providerManager;

        /// <summary>
        /// Test-only switch.
        /// </summary>
        private bool _suppressPartialSemantics;

        internal CompletionServiceWithProviders(Workspace workspace)
        {
            _workspace = workspace;
            _providerManager = new(this);
        }

        /// <summary>
        /// Backward compatibility only.
        /// </summary>
        public sealed override CompletionRules GetRules()
            => GetRules(CompletionOptions.From(_workspace.CurrentSolution.Options, Language));

        /// <summary>
        /// Returns the providers always available to the service.
        /// This does not included providers imported via MEF composition.
        /// </summary>
        [Obsolete("Built-in providers will be ignored in a future release, please make them MEF exports instead.")]
        protected virtual ImmutableArray<CompletionProvider> GetBuiltInProviders()
            => ImmutableArray<CompletionProvider>.Empty;

        internal IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> GetImportedProviders()
            => _providerManager.GetImportedProviders();

        protected ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string>? roles)
            => _providerManager.GetProviders(roles);

        protected virtual ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string>? roles, CompletionTrigger trigger)
            => GetProviders(roles);

        protected internal CompletionProvider? GetProvider(CompletionItem item)
            => _providerManager.GetProvider(item);

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

            return await document.GetPartialSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task<CompletionList?> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles,
            OptionSet? options,
            CancellationToken cancellationToken)
        {
            var completionOptions = CompletionOptions.From(options ?? document.Project.Solution.Options, document.Project.Language);
            return await GetCompletionsWithAvailabilityOfExpandedItemsAsync(document, caretPosition, completionOptions, trigger, roles, cancellationToken).ConfigureAwait(false);
        }

        private protected async Task<CompletionList> GetCompletionsWithAvailabilityOfExpandedItemsAsync(
            Document document,
            int caretPosition,
            CompletionOptions options,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles,
            CancellationToken cancellationToken)
        {
            // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
            (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = GetDefaultCompletionListSpan(text, caretPosition);

            var providers = _providerManager.GetFilteredProviders(document.Project, roles, trigger, options);

            // Phase 1: Completion Providers decide if they are triggered based on textual analysis
            // Phase 2: Completion Providers use syntax to confirm they are triggered, or decide they are not actually triggered and should become an augmenting provider
            // Phase 3: Triggered Providers are asked for items
            // Phase 4: If any items were provided, all augmenting providers are asked for items
            // This allows a provider to be textually triggered but later decide to be an augmenting provider based on deeper syntactic analysis.

            var triggeredProviders = GetTriggeredProviders(document, providers, caretPosition, options, trigger, roles, text);

            var additionalAugmentingProviders = await GetAugmentingProviders(document, triggeredProviders, caretPosition, trigger, options, cancellationToken).ConfigureAwait(false);
            triggeredProviders = triggeredProviders.Except(additionalAugmentingProviders).ToImmutableArray();

            // Now, ask all the triggered providers, in parallel, to populate a completion context.
            // Note: we keep any context with items *or* with a suggested item.  
            var triggeredContexts = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options, defaultItemSpan, triggeredProviders, cancellationToken).ConfigureAwait(false);

            // Nothing to do if we didn't even get any regular items back (i.e. 0 items or suggestion item only.)
            if (!triggeredContexts.ContainsNonSuggestionModeItem)
                return CompletionList.Empty;

            // See if there were completion contexts provided that were exclusive. If so, then that's all we'll return.
            if (triggeredContexts.ContainsExclusiveContext)
                return MergeAndPruneCompletionLists(triggeredContexts.GetExclusiveContexts(), defaultItemSpan, options, isExclusive: true);

            // Great!  We had some items.  Now we want to see if any of the other providers 
            // would like to augment the completion list.  For example, we might trigger
            // enum-completion on space.  If enum completion results in any items, then 
            // we'll want to augment the list with all the regular symbol completion items.
            var augmentingProviders = providers.Except(triggeredProviders).ToImmutableArray();

            var augmentingContexts = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options, defaultItemSpan, augmentingProviders, cancellationToken).ConfigureAwait(false);

            GC.KeepAlive(semanticModel);

            // Providers are ordered, but we processed them in our own order.  Ensure that the
            // groups are properly ordered based on the original providers.
            var completionProviderToIndex = GetCompletionProviderToIndex(providers);
            var allContexts = triggeredContexts.NonEmptyContexts.Concat(augmentingContexts.NonEmptyContexts)
                .Sort((p1, p2) => completionProviderToIndex[p1.Provider] - completionProviderToIndex[p2.Provider]);

            return MergeAndPruneCompletionLists(allContexts, defaultItemSpan, options, isExclusive: false);

            ImmutableArray<CompletionProvider> GetTriggeredProviders(
                Document document, ConcatImmutableArray<CompletionProvider> allProviders, int caretPosition, CompletionOptions options, CompletionTrigger trigger, ImmutableHashSet<string>? roles, SourceText text)
            {
                switch (trigger.Kind)
                {
                    case CompletionTriggerKind.Insertion:
                    case CompletionTriggerKind.Deletion:
                        if (!ShouldTypingTriggerCompletionWithoutAskingProviders(trigger, options))
                            return ImmutableArray<CompletionProvider>.Empty;

                        var triggeredProviders = allProviders.Where(p => p.ShouldTriggerCompletion(document.Project.LanguageServices, text, caretPosition, trigger, options)).ToImmutableArrayOrEmpty();
                        Debug.Assert(ValidatePossibleTriggerCharacterSet(trigger.Kind, triggeredProviders, document, text, caretPosition, options));

                        return triggeredProviders.Length == 0
                            ? allProviders.ToImmutableArray()
                            : triggeredProviders;

                    default:
                        return allProviders.ToImmutableArray();
                }
            }

            static async Task<ImmutableArray<CompletionProvider>> GetAugmentingProviders(
                Document document, ImmutableArray<CompletionProvider> triggeredProviders, int caretPosition, CompletionTrigger trigger, CompletionOptions options, CancellationToken cancellationToken)
            {
                var additionalAugmentingProviders = ArrayBuilder<CompletionProvider>.GetInstance(triggeredProviders.Length);
                if (trigger.Kind == CompletionTriggerKind.Insertion)
                {
                    foreach (var provider in triggeredProviders)
                    {
                        if (!await provider.IsSyntacticTriggerCharacterAsync(document, caretPosition, trigger, options, cancellationToken).ConfigureAwait(false))
                        {
                            additionalAugmentingProviders.Add(provider);
                        }
                    }
                }

                return additionalAugmentingProviders.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Backward compatibility only.
        /// </summary>
        public sealed override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string>? roles = null, OptionSet? options = null)
        {
            var document = text.GetOpenDocumentInCurrentContextWithChanges();
            var languageServices = document?.Project.LanguageServices ?? _workspace.Services.GetLanguageServices(Language);
            var completionOptions = CompletionOptions.From(options ?? document?.Project.Solution.Options ?? _workspace.CurrentSolution.Options, document?.Project.Language ?? Language);
            return ShouldTriggerCompletion(document?.Project, languageServices, text, caretPosition, trigger, completionOptions, roles);
        }

        /// <summary>
        /// A preliminary quick check to see if we should trigger completion before asking each individual providers.
        /// </summary>
        private bool ShouldTypingTriggerCompletionWithoutAskingProviders(CompletionTrigger trigger, CompletionOptions options)
        {
            if (!options.TriggerOnTyping)
            {
                return false;
            }

            if (trigger.Kind == CompletionTriggerKind.Deletion && SupportsTriggerOnDeletion(options))
            {
                return char.IsLetterOrDigit(trigger.Character) || trigger.Character == '.';
            }

            return true;
        }

        internal sealed override bool ShouldTriggerCompletion(
            Project? project, HostLanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, ImmutableHashSet<string>? roles = null)
        {
            if (!ShouldTypingTriggerCompletionWithoutAskingProviders(trigger, options))
                return false;

            var providers = _providerManager.GetFilteredProviders(project, roles, trigger, options);
            return providers.Any(p => p.ShouldTriggerCompletion(languageServices, text, caretPosition, trigger, options));
        }

        internal virtual bool SupportsTriggerOnDeletion(CompletionOptions options)
            => options.TriggerOnDeletion == true;

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

        private static async Task<AggregatedCompletionContextsData> ComputeNonEmptyCompletionContextsAsync(
            Document document, int caretPosition, CompletionTrigger trigger,
            CompletionOptions options, TextSpan defaultItemSpan,
            ImmutableArray<CompletionProvider> providers,
            CancellationToken cancellationToken)
        {
            var completionContextTasks = new List<Task<CompletionContext>>();
            foreach (var provider in providers)
            {
                completionContextTasks.Add(GetContextAsync(
                    provider, document, caretPosition, trigger,
                    options, defaultItemSpan, cancellationToken));
            }

            var completionContexts = await Task.WhenAll(completionContextTasks).ConfigureAwait(false);
            return new(completionContexts);
        }

        private CompletionList MergeAndPruneCompletionLists(
            ImmutableArray<CompletionContext> completionContexts,
            TextSpan defaultSpan,
            in CompletionOptions options,
            bool isExclusive)
        {
            // See if any contexts changed the completion list span.  If so, the first context that
            // changed it 'wins' and picks the span that will be used for all items in the completion
            // list.  If no contexts changed it, then just use the default span provided by the service.
            var finalCompletionListSpan = completionContexts.FirstOrDefault(c => c.CompletionListSpan != defaultSpan)?.CompletionListSpan ?? defaultSpan;
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

            // TODO(DustinCa): Revisit performance of this.
            using var _ = ArrayBuilder<CompletionItem>.GetInstance(displayNameToItemsMap.Count, out var builder);
            builder.AddRange(displayNameToItemsMap);
            builder.Sort();

            return CompletionList.Create(
                finalCompletionListSpan,
                builder.ToImmutable(),
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
                && item.SortText == existingItem.SortText;
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
            CancellationToken cancellationToken)
        {
            var context = new CompletionContext(provider, document, position, defaultSpan, triggerInfo, options, cancellationToken);
            await provider.ProvideCompletionsAsync(context).ConfigureAwait(false);
            return context;
        }

        internal override async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(item);
            if (provider is null)
                return CompletionDescription.Empty;

            // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
            (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
            var description = await provider.GetDescriptionAsync(document, item, options, displayOptions, cancellationToken).ConfigureAwait(false);
            GC.KeepAlive(semanticModel);
            return description;
        }

        public override async Task<CompletionChange> GetChangeAsync(
            Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            var provider = GetProvider(item);
            if (provider != null)
            {
                // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
                (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
                var change = await provider.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
                GC.KeepAlive(semanticModel);
                return change;
            }
            else
            {
                return CompletionChange.Create(new TextChange(item.Span, item.DisplayText));
            }
        }

        private class DisplayNameToItemsMap : IEnumerable<CompletionItem>, IDisposable
        {
            // We might need to handle large amount of items with import completion enabled,
            // so use a dedicated pool to minimize array allocations.
            // Set the size of pool to a small number 5 because we don't expect more than a
            // couple of callers at the same time.
            private static readonly ObjectPool<Dictionary<string, object>> s_uniqueSourcesPool
                = new(factory: () => new(), size: 5);

            private readonly Dictionary<string, object> _displayNameToItemsMap;
            private readonly CompletionServiceWithProviders _service;

            public int Count { get; private set; }

            public DisplayNameToItemsMap(CompletionServiceWithProviders service)
            {
                _service = service;
                _displayNameToItemsMap = s_uniqueSourcesPool.Allocate();
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

        private readonly struct AggregatedCompletionContextsData
        {
            public ImmutableArray<CompletionContext> NonEmptyContexts { get; }

            public bool ContainsNonSuggestionModeItem { get; }
            public bool ContainsExclusiveContext { get; }

            public bool IsEmpty => NonEmptyContexts.Length == 0;

            public AggregatedCompletionContextsData(CompletionContext[] allContexts)
            {
                var containsNonSuggestionModeItem = false;
                var containsExclusiveContext = false;
                var builder = ArrayBuilder<CompletionContext>.GetInstance(allContexts.Length);

                foreach (var context in allContexts)
                {
                    if (context.Items.Count > 0)
                    {
                        containsNonSuggestionModeItem = true;
                        builder.Add(context);
                    }
                    else if (context.SuggestionModeItem != null)
                    {
                        builder.Add(context);
                    }

                    if (context.IsExclusive)
                        containsExclusiveContext = true;
                }

                NonEmptyContexts = builder.ToImmutableAndFree();
                ContainsNonSuggestionModeItem = containsNonSuggestionModeItem;
                ContainsExclusiveContext = containsExclusiveContext;
            }

            public ImmutableArray<CompletionContext> GetExclusiveContexts()
                => ContainsExclusiveContext ? NonEmptyContexts.WhereAsArray(c => c.IsExclusive) : ImmutableArray<CompletionContext>.Empty;
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionServiceWithProviders _completionServiceWithProviders;

            public TestAccessor(CompletionServiceWithProviders completionServiceWithProviders)
                => _completionServiceWithProviders = completionServiceWithProviders;

            internal ImmutableArray<CompletionProvider> GetAllProviders(ImmutableHashSet<string> roles)
                => _completionServiceWithProviders._providerManager.GetAllProviders(roles);

            internal async Task<CompletionContext> GetContextAsync(
                CompletionProvider provider,
                Document document,
                int position,
                CompletionTrigger triggerInfo,
                CompletionOptions options,
                CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var defaultItemSpan = _completionServiceWithProviders.GetDefaultCompletionListSpan(text, position);

                return await CompletionServiceWithProviders.GetContextAsync(
                    provider,
                    document,
                    position,
                    triggerInfo,
                    options,
                    defaultItemSpan,
                    cancellationToken).ConfigureAwait(false);
            }

            public void SuppressPartialSemantics()
                => _completionServiceWithProviders._suppressPartialSemantics = true;
        }
    }
}
