using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;


namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A subtype of <see cref="CompletionService"/> that aggregates completions from one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    public abstract class CompletionServiceWithProviders : CompletionService, IEqualityComparer<ImmutableHashSet<string>>
    {
        private static readonly Func<string, List<CompletionItem>> s_createList = _ => new List<CompletionItem>();

        private readonly object _gate = new object();

        private readonly Dictionary<string, CompletionProvider> _nameToProvider = new Dictionary<string, CompletionProvider>();
        private readonly Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _rolesToProviders;
        private readonly Func<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>> _createRoleProviders;

        private readonly Workspace _workspace;

        /// <summary>
        /// Internal for testing purposes.
        /// </summary>
        internal readonly ImmutableArray<CompletionProvider>? ExclusiveProviders;

        private IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> _importedProviders;

        protected CompletionServiceWithProviders(Workspace workspace)
            : this(workspace, exclusiveProviders: null)
        {
        }

        internal CompletionServiceWithProviders(
            Workspace workspace,
            ImmutableArray<CompletionProvider>? exclusiveProviders = null)
        {
            _workspace = workspace;
            ExclusiveProviders = exclusiveProviders;
            _rolesToProviders = new Dictionary<ImmutableHashSet<string>, ImmutableArray<CompletionProvider>>(this);
            _createRoleProviders = CreateRoleProviders;
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
        private object p;

        internal void SetTestProviders(IEnumerable<CompletionProvider> testProviders)
        {
            lock (_gate)
            {
                _testProviders = testProviders != null ? testProviders.ToImmutableArray() : ImmutableArray<CompletionProvider>.Empty;
                _rolesToProviders.Clear();
                _nameToProvider.Clear();
            }
        }

        private ImmutableArray<CompletionProvider> CreateRoleProviders(ImmutableHashSet<string> roles)
        {
            var providers = GetAllProviders(roles);

            foreach (var provider in providers)
            {
                _nameToProvider[provider.Name] = provider;
            }

            return providers;
        }

        private ImmutableArray<CompletionProvider> GetAllProviders(ImmutableHashSet<string> roles)
        {
            if (ExclusiveProviders.HasValue)
            {
                return ExclusiveProviders.Value;
            }

            var builtin = GetBuiltInProviders();
            var imported = GetImportedProviders()
                .Where(lz => lz.Metadata.Roles == null || lz.Metadata.Roles.Length == 0 || roles.Overlaps(lz.Metadata.Roles))
                .Select(lz => lz.Value);

            var providers = builtin.Concat(imported).Concat(_testProviders);
            return providers.ToImmutableArray();
        }

        protected ImmutableArray<CompletionProvider> GetProviders(ImmutableHashSet<string> roles)
        {
            roles = roles ?? ImmutableHashSet<string>.Empty;

            lock (_gate)
            {
                return _rolesToProviders.GetOrAdd(roles, _createRoleProviders);
            }
        }

        protected virtual ImmutableArray<CompletionProvider> GetProviders(
            ImmutableHashSet<string> roles, CompletionTrigger trigger)
        {
            var snippetsRule = this.GetRules().SnippetsRule;

            if (snippetsRule == SnippetsRule.Default ||
                snippetsRule == SnippetsRule.NeverInclude)
            {
                return GetProviders(roles).Where(p => !p.IsSnippetProvider).ToImmutableArray();
            }
            else if (snippetsRule == SnippetsRule.AlwaysInclude)
            {
                return GetProviders(roles);
            }
            else if (snippetsRule == SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                if (trigger.Kind == CompletionTriggerKind.Snippets)
                {
                    return GetProviders(roles).Where(p => p.IsSnippetProvider).ToImmutableArray();
                }
                else
                {
                    return GetProviders(roles).Where(p => !p.IsSnippetProvider).ToImmutableArray();
                }
            }

            return ImmutableArray<CompletionProvider>.Empty;
        }

        internal protected CompletionProvider GetProvider(CompletionItem item)
        {
            string name;
            CompletionProvider provider = null;

            if (item.Properties.TryGetValue("Provider", out name))
            {
                lock (_gate)
                {
                    _nameToProvider.TryGetValue(name, out provider);
                }
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
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = this.GetDefaultCompletionListSpan(text, caretPosition);

            options = options ?? document.Options;
            var providers = GetProviders(roles, trigger);

            var completionProviderToIndex = GetCompletionProviderToIndex(providers);

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
            var completionContexts = new List<CompletionContext>();
            foreach (var provider in triggeredProviders)
            {
                var completionContext = await GetContextAsync(
                    provider, document, caretPosition, trigger, 
                    options, defaultItemSpan, cancellationToken).ConfigureAwait(false);
                if (completionContext != null)
                {
                    completionContexts.Add(completionContext);
                }
            }

            // See if there was a group provided that was exclusive and had items in it.  If so, then
            // that's all we'll return.
            var firstExclusiveContext = completionContexts.FirstOrDefault(t => t.IsExclusive && t.Items.Any());

            if (firstExclusiveContext != null)
            {
                return MergeAndPruneCompletionLists(
                    SpecializedCollections.SingletonEnumerable(firstExclusiveContext), defaultItemSpan,
                    isExclusive: true);
            }

            // If no exclusive providers provided anything, then go through the remaining
            // triggered list and see if any provide items.
            var nonExclusiveLists = completionContexts.Where(t => !t.IsExclusive).ToList();

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
                var completionList = await GetContextAsync(provider, document, caretPosition, trigger, options, defaultItemSpan, cancellationToken).ConfigureAwait(false);
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

            return MergeAndPruneCompletionLists(allProvidersAndLists, defaultItemSpan, isExclusive: false);
        }

        private CompletionList MergeAndPruneCompletionLists(
            IEnumerable<CompletionContext> completionLists, TextSpan contextSpan, bool isExclusive)
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

            return CompletionList.Create(
                contextSpan, totalItems.ToImmutableArray(), this.GetRules(), suggestionModeItem,
                isExclusive);
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

        // Internal for testing purposes only.
        internal async Task<CompletionContext> GetContextAsync(
            CompletionProvider provider,
            Document document,
            int position,
            CompletionTrigger triggerInfo,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            return await GetContextAsync(
                provider, document, position, triggerInfo, 
                options, defaultSpan: null, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            options = options ?? document.Project.Solution.Workspace.Options;

            if (defaultSpan == null)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                defaultSpan = this.GetDefaultCompletionListSpan(text, position);
            }

            var context = new CompletionContext(provider, document, position, defaultSpan.Value, triggerInfo, options, cancellationToken);
            await provider.ProvideCompletionsAsync(context).ConfigureAwait(false);
            return context;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken = default(CancellationToken))
        {
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

        public override bool ShouldTriggerCompletion(
            SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            options = options ?? _workspace.Options;
            if (!options.GetOption(CompletionOptions.TriggerOnTyping, this.Language))
            {
                return false;
            }

            if (trigger.Kind == CompletionTriggerKind.Deletion && this.SupportsTriggerOnDeletion(options))
            {
                return Char.IsLetterOrDigit(trigger.Character) || trigger.Character == '.';
            }

            var providers = this.GetProviders(roles, CompletionTrigger.Default);
            return providers.Any(p => p.ShouldTriggerCompletion(text, caretPosition, trigger, options));
        }

        internal virtual bool SupportsTriggerOnDeletion(OptionSet options)
        {
            var opt = options.GetOption(CompletionOptions.TriggerOnDeletion, this.Language);
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

        bool IEqualityComparer<ImmutableHashSet<string>>.Equals(ImmutableHashSet<string> x, ImmutableHashSet<string> y)
        {
            if (x == y)
            {
                return true;
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (var v in x)
            {
                if (!y.Contains(v))
                {
                    return false;
                }
            }

            return true;
        }

        int IEqualityComparer<ImmutableHashSet<string>>.GetHashCode(ImmutableHashSet<string> obj)
        {
            var hash = 0;
            foreach (var o in obj)
            {
                hash += o.GetHashCode();
            }

            return hash;
        }
    }
}
