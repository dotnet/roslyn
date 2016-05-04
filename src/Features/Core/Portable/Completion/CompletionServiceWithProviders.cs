using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;


namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A subtype of <see cref="CompletionService"/> that aggregates completions from one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    public abstract class CompletionServiceWithProviders : CompletionService
    {
        private static readonly Func<string, List<CompletionItem>> s_createList = _ => new List<CompletionItem>();
        private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> _importedProviders;
        private readonly Workspace _workspace;

        protected CompletionServiceWithProviders(Workspace workspace)
        {
            _workspace = workspace;
        }

        public override CompletionRules GetRules()
        {
            return CompletionRules.Default;
        }

        /// <summary>
        /// Returns the providers always available to the service.
        /// This does not included providers imported via MEF composition.
        /// </summary>
        protected virtual ImmutableArray<CompletionProvider> GetBuiltInProviders()
        {
            return ImmutableArray<CompletionProvider>.Empty;
        }

        private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> GetImportedProviders()
        {
            if (_importedProviders == null)
            {
                var language = this.Language;
                var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<CompletionProvider, CompletionProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == language)
                        ).ToList();

                Interlocked.CompareExchange(ref _importedProviders, providers, null);
            }

            return _importedProviders;
        }

        private ImmutableArray<CompletionProvider> _testProviders = ImmutableArray<CompletionProvider>.Empty;

        internal void SetTestProviders(IEnumerable<CompletionProvider> testProviders)
        {
            _testProviders = testProviders != null ? testProviders.ToImmutableArray() : ImmutableArray<CompletionProvider>.Empty;
            _lazyNameToProviderMap = null;
        }

        private class RoleProviders
        {
            public ImmutableArray<CompletionProvider> Providers;
        }

        private readonly ConditionalWeakTable<ImmutableHashSet<string>, RoleProviders> _roleProviders
            = new ConditionalWeakTable<ImmutableHashSet<string>, RoleProviders>();

        protected ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles)
        {
            roles = roles ?? ImmutableHashSet<string>.Empty;

            RoleProviders providers;
            if (!_roleProviders.TryGetValue(roles, out providers))
            {
                providers = _roleProviders.GetValue(roles, _ =>
                {
                    var builtin = GetBuiltInProviders();
                    var imported = GetImportedProviders()
                        .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                        .Select(lz => lz.Value);
                    return new RoleProviders { Providers = builtin.Concat(imported).ToImmutableArray() };
                });
            }

            if (_testProviders.Length > 0)
            {
                return providers.Providers.Concat(_testProviders);
            }
            else
            {
                return providers.Providers;
            }
        }

        protected virtual ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles, CompletionTrigger trigger)
        {
            if (trigger.Kind == CompletionTriggerKind.Snippets)
            {
                return GetProviders(roles).Where(p => p.IsSnippetProvider).ToImmutableArray();
            }
            else
            {
                return GetProviders(roles);
            }
        }

        private ImmutableDictionary<string, CompletionProvider> _lazyNameToProviderMap = null;
        private ImmutableDictionary<string, CompletionProvider> NameToProviderMap
        {
            get
            {
                if (_lazyNameToProviderMap == null)
                {
                    Interlocked.CompareExchange(ref _lazyNameToProviderMap, CreateNameToProviderMap(), null);
                }

                return _lazyNameToProviderMap;
            }
        }

        private ImmutableDictionary<string, CompletionProvider> CreateNameToProviderMap()
        {
            var map = ImmutableDictionary<string, CompletionProvider>.Empty;

            foreach (var provider in GetBuiltInProviders().Concat(GetImportedProviders().Select(lz => lz.Value)).Concat(_testProviders))
            {
                if (!map.ContainsKey(provider.Name))
                {
                    map = map.Add(provider.Name, provider);
                }
            }

            return map;
        }

        internal protected CompletionProvider GetProvider(CompletionItem item)
        {
            string name;
            CompletionProvider provider;

            if (item.Properties.TryGetValue("Provider", out name)
                && this.NameToProviderMap.TryGetValue(name, out provider))
            {
                return provider;
            }

            return null;
        }

        public override async Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string> roles,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = this.GetDefaultItemSpan(text, caretPosition);

            options = options ?? document.Project.Solution.Workspace.Options;
            var providers = GetProviders(roles, trigger);

            var completionProviderToIndex = GetCompletionProviderToIndex(providers);
            var completionRules = this.GetRules();

            var triggeredProviders = ImmutableArray<CompletionProvider>.Empty;
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion:
                case CompletionTriggerKind.Deletion:
                    if (this.ShouldTriggerCompletion(text, caretPosition, trigger, roles, options))
                    {
                        triggeredProviders = providers.Where(p => p.ShouldTriggerCompletion(text, caretPosition, trigger, options)).ToImmutableArrayOrEmpty();
                        if (triggeredProviders.Length == 0)
                        {
                            triggeredProviders = providers;
                        }
                    }
                    break;
                default:
                    triggeredProviders = providers;
                    break;
            }

            // Now, ask all the triggered providers if they can provide a group.
            var completionLists = new List<CompletionContext>();
            foreach (var provider in triggeredProviders)
            {
                var completionList = await GetProviderCompletionsAsync(provider, document, caretPosition, defaultItemSpan, trigger, options, cancellationToken).ConfigureAwait(false);
                if (completionList != null)
                {
                    completionLists.Add(completionList);
                }
            }

            // See if there was a group provided that was exclusive and had items in it.  If so, then
            // that's all we'll return.
            var firstExclusiveList = completionLists.FirstOrDefault(t => t.IsExclusive && t.Items.Any());

            if (firstExclusiveList != null)
            {
                return MergeAndPruneCompletionLists(SpecializedCollections.SingletonEnumerable(firstExclusiveList), defaultItemSpan);
            }

            // If no exclusive providers provided anything, then go through the remaining
            // triggered list and see if any provide items.
            var nonExclusiveLists = completionLists.Where(t => !t.IsExclusive).ToList();

            // If we still don't have any items, then we're definitely done.
            if (!nonExclusiveLists.Any(g => g.Items.Any()))
            {
                return null;
            }

            // If we do have items, then ask all the other (non exclusive providers) if they
            // want to augment the items.
            var usedProviders = nonExclusiveLists.Select(g => g.Provider);
            var nonUsedProviders = providers.Except(usedProviders);
            var nonUsedNonExclusiveLists = new List<CompletionContext>();
            foreach (var provider in nonUsedProviders)
            {
                var completionList = await GetProviderCompletionsAsync(provider, document, caretPosition, defaultItemSpan, trigger, options, cancellationToken).ConfigureAwait(false);
                if (completionList != null && !completionList.IsExclusive)
                {
                    nonUsedNonExclusiveLists.Add(completionList);
                }
            }

            var allProvidersAndLists = nonExclusiveLists.Concat(nonUsedNonExclusiveLists).ToList();
            if (allProvidersAndLists.Count == 0)
            {
                return null;
            }

            // Providers are ordered, but we processed them in our own order.  Ensure that the
            // groups are properly ordered based on the original providers.
            allProvidersAndLists.Sort((p1, p2) => completionProviderToIndex[p1.Provider] - completionProviderToIndex[p2.Provider]);

            return MergeAndPruneCompletionLists(allProvidersAndLists, defaultItemSpan);
        }

        private CompletionList MergeAndPruneCompletionLists(IEnumerable<CompletionContext> completionLists, TextSpan contextSpan)
        {
            var displayNameToItemsMap = new Dictionary<string, List<CompletionItem>>();
            CompletionItem suggestionModeItem = null;

            foreach (var completionList in completionLists)
            {
                Contract.Assert(completionList != null);

                foreach (var item in completionList.Items)
                {
                    Contract.Assert(item != null);
                    AddToDisplayMap(item, displayNameToItemsMap);
                }

                // first one wins
                suggestionModeItem = suggestionModeItem ?? completionList.SuggestionModeItem;
            }

            if (displayNameToItemsMap.Count == 0)
            {
                return CompletionList.Empty;
            }

            // TODO(DustinCa): Revisit performance of this.
            var totalItems = displayNameToItemsMap.Values.Flatten().ToList();
            totalItems.Sort();

            return CompletionList.Create(contextSpan, totalItems.ToImmutableArray(), this.GetRules(), suggestionModeItem);
        }

        private void AddToDisplayMap(
            CompletionItem item,
            Dictionary<string, List<CompletionItem>> displayNameToItemsMap)
        {
            var sameNamedItems = displayNameToItemsMap.GetOrAdd(item.DisplayText, s_createList);

            // If two items have the same display text choose which one to keep.
            // If they don't actually match keep both.

            for (int i = 0; i < sameNamedItems.Count; i++)
            {
                var existingItem = sameNamedItems[i];

                Contract.Assert(item.DisplayText == existingItem.DisplayText);

                if (ItemsMatch(item, existingItem))
                {
                    sameNamedItems[i] = GetBetterItem(item, existingItem);
                    return;
                }
            }

            sameNamedItems.Add(item);
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

        private Dictionary<CompletionProvider, int> GetCompletionProviderToIndex(IEnumerable<CompletionProvider> completionProviders)
        {
            var result = new Dictionary<CompletionProvider, int>();

            int i = 0;
            foreach (var completionProvider in completionProviders)
            {
                result[completionProvider] = i;
                i++;
            }

            return result;
        }

        private static async Task<CompletionContext> GetProviderCompletionsAsync(
            CompletionProvider provider,
            Document document,
            int position,
            TextSpan defaultFilterSpan,
            CompletionTrigger triggerInfo,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var context = new CompletionContext(provider, document, position, defaultFilterSpan, triggerInfo, options, cancellationToken);
            await provider.ProvideCompletionsAsync(context).ConfigureAwait(false);
            return context;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (CommonCompletionItem.HasDescription(item))
            {
                return Task.FromResult(CommonCompletionItem.GetDescription(item));
            }

            var provider = GetProvider(item);
            if (provider != null)
            {
                return provider.GetDescriptionAsync(document, item, cancellationToken);
            }
            else
            {
                return Task.FromResult(CompletionDescription.Empty);
            }
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            options = options ?? _workspace.Options;
            if (!options.GetOption(CompletionOptions.TriggerOnTyping, this.Language))
            {
                return false;
            }

            var providers = this.GetProviders(roles, CompletionTrigger.Default);
            return providers.Any(p => p.ShouldTriggerCompletion(text, caretPosition, trigger, options));
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            var provider = GetProvider(item);
            if (provider != null)
            {
                return await provider.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return CompletionChange.Create(ImmutableArray.Create(new TextChange(item.Span, item.DisplayText)));
            }
        }
    }
}
