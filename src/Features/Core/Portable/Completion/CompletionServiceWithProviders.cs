// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        private readonly ProviderSource _providerSource;

        protected CompletionServiceWithProviders(Workspace workspace)
        {
            _workspace = workspace;
            _providerSource = new ProviderSource(this);
        }

        public override CompletionRules GetRules()
            => CompletionRules.Default;

        /// <summary>
        /// Returns the providers always available to the service.
        /// This does not included providers imported via MEF composition.
        /// </summary>
        [Obsolete("Built-in providers will be ignored in a future release, please make them MEF exports instead.")]
        protected virtual ImmutableArray<CompletionProvider> GetBuiltInProviders()
            => ImmutableArray<CompletionProvider>.Empty;

        protected ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles)
            => _providerSource.GetProvidersAsync(roles ?? ImmutableHashSet<string>.Empty).Result;

        [Obsolete("Please use GetProvidersAsync instead.")]
        protected virtual ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles, CompletionTrigger trigger)
            => GetProviders(roles);

        protected Task<ImmutableArray<CompletionProvider>> GetProvidersAsync(ImmutableHashSet<string> roles)
            => _providerSource.GetProvidersAsync(roles ?? ImmutableHashSet<string>.Empty);

        protected virtual Task<ImmutableArray<CompletionProvider>> GetProvidersAsync(
            ImmutableHashSet<string> roles,
            CompletionTrigger trigger,
            CancellationToken cancellationToken = default)
            => GetProvidersAsync(roles);

        private async Task<ConcatImmutableArray<CompletionProvider>> GetFilteredProvidersAsync(
            Project project, ImmutableHashSet<string> roles, CompletionTrigger trigger, OptionSet options)
        {
            var providers = await GetProvidersAsync(roles, trigger).ConfigureAwait(false);
            var allCompletionProviders = FilterProviders(providers, trigger, options);
            var projectCompletionProviders = FilterProviders(GetProjectCompletionProviders(project), trigger, options);
            return allCompletionProviders.ConcatFast(projectCompletionProviders);
        }

        private ImmutableArray<CompletionProvider> FilterProviders(
            ImmutableArray<CompletionProvider> providers,
            CompletionTrigger trigger,
            OptionSet options)
        {
            if (options.GetOption(CompletionServiceOptions.IsExpandedCompletion))
            {
                providers = providers.WhereAsArray(p => p.IsExpandItemProvider);
            }

            // If the caller passed along specific options that affect snippets,
            // then defer to those.  Otherwise if the caller just wants the default
            // behavior, then get the snippets behavior from our own rules.
            var optionsRule = options.GetOption(CompletionOptions.SnippetsBehavior, Language);
            var snippetsRule = optionsRule != SnippetsRule.Default
                ? optionsRule
                : GetRules().SnippetsRule;

            if (snippetsRule is SnippetsRule.Default or
                SnippetsRule.NeverInclude)
            {
                return providers.Where(p => !p.IsSnippetProvider).ToImmutableArray();
            }
            else if (snippetsRule == SnippetsRule.AlwaysInclude)
            {
                return providers;
            }
            else if (snippetsRule == SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                if (trigger.Kind == CompletionTriggerKind.Snippets)
                {
                    return providers.Where(p => p.IsSnippetProvider).ToImmutableArray();
                }
                else
                {
                    return providers.Where(p => !p.IsSnippetProvider).ToImmutableArray();
                }
            }

            return ImmutableArray<CompletionProvider>.Empty;
        }

        protected internal CompletionProvider GetProvider(CompletionItem item)
        {
            CompletionProvider provider = null;

            if (item.ProviderName != null)
            {
                provider = _providerSource.GetProviderByName(item.ProviderName);
            }

            return provider;
        }

        public override async Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string> roles,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var (completionList, _) = await GetCompletionsWithAvailabilityOfExpandedItemsAsync(document, caretPosition, trigger, roles, options, cancellationToken).ConfigureAwait(false);
            return completionList;
        }

        private protected async Task<(CompletionList completionList, bool expandItemsAvailable)> GetCompletionsWithAvailabilityOfExpandedItemsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string> roles,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = GetDefaultCompletionListSpan(text, caretPosition);

            options ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var providers = await GetFilteredProvidersAsync(document.Project, roles, trigger, options).ConfigureAwait(false);

            var completionProviderToIndex = GetCompletionProviderToIndex(providers);

            var triggeredProviders = ImmutableArray<CompletionProvider>.Empty;
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion:
                case CompletionTriggerKind.Deletion:
                    if (ShouldTriggerCompletion(document.Project, text, caretPosition, trigger, roles, options))
                    {
                        triggeredProviders = providers.Where(p => p.ShouldTriggerCompletion(document.Project.LanguageServices, text, caretPosition, trigger, options)).ToImmutableArrayOrEmpty();
                        Debug.Assert(ValidatePossibleTriggerCharacterSet(trigger.Kind, triggeredProviders, document, text, caretPosition, options));
                        if (triggeredProviders.Length == 0)
                        {
                            triggeredProviders = providers.ToImmutableArray();
                        }
                    }

                    break;
                default:
                    triggeredProviders = providers.ToImmutableArray();
                    break;
            }

            // Phase 1: Completion Providers decide if they are triggered based on textual analysis
            // Phase 2: Completion Providers use syntax to confirm they are triggered, or decide they are not actually triggered and should become an augmenting provider
            // Phase 3: Triggered Providers are asked for items
            // Phase 4: If any items were provided, all augmenting providers are asked for items
            // This allows a provider to be textually triggered but later decide to be an augmenting provider based on deeper syntactic analysis.

            var additionalAugmentingProviders = new List<CompletionProvider>();
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

            triggeredProviders = triggeredProviders.Except(additionalAugmentingProviders).ToImmutableArray();

            // Now, ask all the triggered providers, in parallel, to populate a completion context.
            // Note: we keep any context with items *or* with a suggested item.  
            var (triggeredCompletionContexts, expandItemsAvailableFromTriggeredProviders) = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options,
                defaultItemSpan, triggeredProviders,
                cancellationToken).ConfigureAwait(false);

            // If we didn't even get any back with items, then there's nothing to do.
            // i.e. if only got items back that had only suggestion items, then we don't
            // want to show any completion.
            if (!triggeredCompletionContexts.Any(cc => cc.Items.Count > 0))
            {
                return (null, expandItemsAvailableFromTriggeredProviders);
            }

            // All the contexts should be non-empty or have a suggestion item.
            Debug.Assert(triggeredCompletionContexts.All(HasAnyItems));

            // See if there were completion contexts provided that were exclusive. If so, then
            // that's all we'll return.
            var exclusiveContexts = triggeredCompletionContexts.Where(t => t.IsExclusive);

            if (exclusiveContexts.Any())
            {
                return (MergeAndPruneCompletionLists(exclusiveContexts, defaultItemSpan, isExclusive: true),
                    expandItemsAvailableFromTriggeredProviders);
            }

            // Shouldn't be any exclusive completion contexts at this point.
            Debug.Assert(triggeredCompletionContexts.All(cc => !cc.IsExclusive));

            // Great!  We had some items.  Now we want to see if any of the other providers 
            // would like to augment the completion list.  For example, we might trigger
            // enum-completion on space.  If enum completion results in any items, then 
            // we'll want to augment the list with all the regular symbol completion items.
            var augmentingProviders = providers.Except(triggeredProviders).ToImmutableArray();

            var (augmentingCompletionContexts, expandItemsAvailableFromAugmentingProviders) = await ComputeNonEmptyCompletionContextsAsync(
                document, caretPosition, trigger, options, defaultItemSpan,
                augmentingProviders, cancellationToken).ConfigureAwait(false);

            var allContexts = triggeredCompletionContexts.Concat(augmentingCompletionContexts);
            Debug.Assert(allContexts.Length > 0);

            // Providers are ordered, but we processed them in our own order.  Ensure that the
            // groups are properly ordered based on the original providers.
            allContexts = allContexts.Sort((p1, p2) => completionProviderToIndex[p1.Provider] - completionProviderToIndex[p2.Provider]);

            return (MergeAndPruneCompletionLists(allContexts, defaultItemSpan, isExclusive: false),
                (expandItemsAvailableFromTriggeredProviders || expandItemsAvailableFromAugmentingProviders));
        }

        private static bool ValidatePossibleTriggerCharacterSet(CompletionTriggerKind completionTriggerKind, IEnumerable<CompletionProvider> triggeredProviders,
            Document document, SourceText text, int caretPosition, OptionSet optionSet)
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
                    if (provider is LSPCompletionProvider lspProvider && lspProvider.IsInsertionTrigger(text, caretPosition - 1, optionSet))
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

        private async Task<(ImmutableArray<CompletionContext>, bool)> ComputeNonEmptyCompletionContextsAsync(
            Document document, int caretPosition, CompletionTrigger trigger,
            OptionSet options, TextSpan defaultItemSpan,
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
            var nonEmptyContexts = completionContexts.Where(HasAnyItems).ToImmutableArray();
            var shouldShowExpander = completionContexts.Any(context => context.ExpandItemsAvailable);
            return (nonEmptyContexts, shouldShowExpander);
        }

        private CompletionList MergeAndPruneCompletionLists(
            IEnumerable<CompletionContext> completionContexts,
            TextSpan defaultSpan,
            bool isExclusive)
        {
            // See if any contexts changed the completion list span.  If so, the first context that
            // changed it 'wins' and picks the span that will be used for all items in the completion
            // list.  If no contexts changed it, then just use the default span provided by the service.
            var finalCompletionListSpan = completionContexts.FirstOrDefault(c => c.CompletionListSpan != defaultSpan)?.CompletionListSpan ?? defaultSpan;
            using var displayNameToItemsMap = new DisplayNameToItemsMap(this);
            CompletionItem suggestionModeItem = null;

            foreach (var context in completionContexts)
            {
                Debug.Assert(context != null);

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
                GetRules(),
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

        private async Task<CompletionContext> GetContextAsync(
            CompletionProvider provider,
            Document document,
            int position,
            CompletionTrigger triggerInfo,
            OptionSet options,
            TextSpan? defaultSpan,
            CancellationToken cancellationToken)
        {
            options ??= document.Project.Solution.Workspace.Options;

            if (defaultSpan == null)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                defaultSpan = GetDefaultCompletionListSpan(text, position);
            }

            var context = new CompletionContext(provider, document, position, defaultSpan.Value, triggerInfo, options, cancellationToken);
            await provider.ProvideCompletionsAsync(context).ConfigureAwait(false);
            return context;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(item);
            return provider != null
                ? provider.GetDescriptionAsync(document, item, cancellationToken)
                : Task.FromResult(CompletionDescription.Empty);
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            var document = text.GetOpenDocumentInCurrentContextWithChanges();
            return ShouldTriggerCompletion(document?.Project, text, caretPosition, trigger, roles, options);
        }

        internal override bool ShouldTriggerCompletion(
            Project project, SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            options ??= _workspace.Options;
            if (!options.GetOption(CompletionOptions.TriggerOnTyping, Language))
            {
                return false;
            }

            if (trigger.Kind == CompletionTriggerKind.Deletion && SupportsTriggerOnDeletion(options))
            {
                return Char.IsLetterOrDigit(trigger.Character) || trigger.Character == '.';
            }

            // MEF importing providers can be expensive, we might not want to block on it in certain scenarios (e.g. when on UI thread)
            var getProvidersTask = GetFilteredProvidersAsync(project, roles, trigger, options);
            if (getProvidersTask.IsCompleted || options.GetOption(CompletionServiceOptions.WaitForProviderCreation))
            {
                return getProvidersTask.Result.Any(p => p.ShouldTriggerCompletion(project?.LanguageServices, text, caretPosition, trigger, options));
            }

            return false;
        }

        internal virtual bool SupportsTriggerOnDeletion(OptionSet options)
        {
            var opt = options.GetOption(CompletionOptions.TriggerOnDeletion, Language);
            return opt == true;
        }

        public override async Task<CompletionChange> GetChangeAsync(
            Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            var provider = GetProvider(item);
            if (provider != null)
            {
                return await provider.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
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

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionServiceWithProviders _completionServiceWithProviders;

            public TestAccessor(CompletionServiceWithProviders completionServiceWithProviders)
                => _completionServiceWithProviders = completionServiceWithProviders;

            internal ImmutableArray<CompletionProvider> GetAllProviders(ImmutableHashSet<string> roles)
                => _completionServiceWithProviders.GetProvidersAsync(roles).Result;

            internal Task<CompletionContext> GetContextAsync(
                CompletionProvider provider,
                Document document,
                int position,
                CompletionTrigger triggerInfo,
                OptionSet options,
                CancellationToken cancellationToken)
            {
                return _completionServiceWithProviders.GetContextAsync(
                    provider,
                    document,
                    position,
                    triggerInfo,
                    options,
                    defaultSpan: null,
                    cancellationToken);
            }
        }
    }
}
