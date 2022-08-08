// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A per language service for constructing context dependent list of completions that 
    /// can be presented to a user during typing in an editor. It aggregates completions from
    /// one or more <see cref="CompletionProvider"/>s.
    /// </summary>
    public abstract partial class CompletionService : ILanguageService
    {
        private readonly SolutionServices _services;
        private readonly ProviderManager _providerManager;

        /// <summary>
        /// Test-only switch.
        /// </summary>
        private bool _suppressPartialSemantics;

        // Prevent inheritance outside of Roslyn.
        internal CompletionService(SolutionServices services)
        {
            _services = services;
            _providerManager = new(this);
        }

        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static CompletionService? GetService(Document? document)
            => document?.GetLanguageService<CompletionService>();

        /// <summary>
        /// Returns the providers always available to the service.
        /// This does not included providers imported via MEF composition.
        /// </summary>
        [Obsolete("Built-in providers will be ignored in a future release, please make them MEF exports instead.")]
        protected virtual ImmutableArray<CompletionProvider> GetBuiltInProviders()
            => ImmutableArray<CompletionProvider>.Empty;

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets the current presentation and behavior rules.
        /// </summary>
        /// <remarks>
        /// Backward compatibility only.
        /// </remarks>
        public CompletionRules GetRules()
        {
            Debug.Fail("For backwards API compat only, should not be called");

            // Publicly available options do not affect this API.
            return GetRules(CompletionOptions.Default);
        }

        internal abstract CompletionRules GetRules(CompletionOptions options);

        /// <summary>
        /// Returns true if the character recently inserted or deleted in the text should trigger completion.
        /// </summary>
        /// <param name="text">The document text to trigger completion within </param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The potential triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <remarks>
        /// This API uses SourceText instead of Document so implementations can only be based on text, not syntax or semantics.
        /// </remarks>
        public bool ShouldTriggerCompletion(
            SourceText text,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles = null,
            OptionSet? options = null)
        {
            var document = text.GetOpenDocumentInCurrentContextWithChanges();
            var languageServices = document?.Project.Services ?? _services.GetLanguageServices(Language);

            // Publicly available options do not affect this API.
            var completionOptions = CompletionOptions.Default;
            var passThroughOptions = options ?? document?.Project.Solution.Options ?? OptionValueSet.Empty;

            return ShouldTriggerCompletion(document?.Project, languageServices, text, caretPosition, trigger, completionOptions, passThroughOptions, roles);
        }

        internal virtual bool SupportsTriggerOnDeletion(CompletionOptions options)
            => options.TriggerOnDeletion == true;

        /// <summary>
        /// Returns true if the character recently inserted or deleted in the text should trigger completion.
        /// </summary>
        /// <param name="project">The project containing the document and text</param>
        /// <param name="languageServices">Language services</param>
        /// <param name="text">The document text to trigger completion within </param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The potential triggering action.</param>
        /// <param name="options">Options.</param>
        /// <param name="passThroughOptions">Options originating either from external caller of the <see cref="CompletionService"/> or set externally to <see cref="Solution.Options"/>.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <remarks>
        /// We pass the project here to retrieve information about the <see cref="Project.AnalyzerReferences"/>,
        /// <see cref="WorkspaceKind"/> and <see cref="Project.Language"/> which are fast operations.
        /// It should not be used for syntactic or semantic operations.
        /// </remarks>
        internal virtual bool ShouldTriggerCompletion(
            Project? project,
            LanguageServices languageServices,
            SourceText text,
            int caretPosition,
            CompletionTrigger trigger,
            CompletionOptions options,
            OptionSet passThroughOptions,
            ImmutableHashSet<string>? roles = null)
        {
            if (!options.TriggerOnTyping)
            {
                return false;
            }

            if (trigger.Kind == CompletionTriggerKind.Deletion && SupportsTriggerOnDeletion(options))
            {
                return char.IsLetterOrDigit(trigger.Character) || trigger.Character == '.';
            }

            var providers = _providerManager.GetFilteredProviders(project, roles, trigger, options);
            return providers.Any(p => p.ShouldTriggerCompletion(languageServices, text, caretPosition, trigger, options, passThroughOptions));
        }

        /// <summary>
        /// Gets the span of the syntax element at the caret position.
        /// This is the most common value used for <see cref="CompletionItem.Span"/>.
        /// </summary>
        /// <param name="text">The document text that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret within the text.</param>
        [Obsolete("Not used anymore. CompletionService.GetDefaultCompletionListSpan is used instead.", error: true)]
        public virtual TextSpan GetDefaultItemSpan(SourceText text, int caretPosition)
            => GetDefaultCompletionListSpan(text, caretPosition);

        public virtual TextSpan GetDefaultCompletionListSpan(SourceText text, int caretPosition)
        {
            return CommonCompletionUtilities.GetWordSpan(
                text, caretPosition, char.IsLetter, char.IsLetterOrDigit);
        }

        /// <summary>
        /// Gets the description of the item.
        /// </summary>
        /// <param name="document">This will be the  original document that
        /// <paramref name="item"/> was created against.</param>
        /// <param name="item">The item to get the description for.</param>
        /// <param name="cancellationToken"></param>
        public Task<CompletionDescription?> GetDescriptionAsync(
            Document document,
            CompletionItem item,
            CancellationToken cancellationToken = default)
        {
            // Publicly available options do not affect this API.
            return GetDescriptionAsync(document, item, CompletionOptions.Default, SymbolDescriptionOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Gets the description of the item.
        /// </summary>
        /// <param name="document">This will be the  original document that
        /// <paramref name="item"/> was created against.</param>
        /// <param name="item">The item to get the description for.</param>
        /// <param name="options">Completion options</param>
        /// <param name="displayOptions">Display options</param>
        /// <param name="cancellationToken"></param>
        internal virtual async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(item, document.Project);
            if (provider is null)
                return CompletionDescription.Empty;

            // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
            (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
            var description = await provider.GetDescriptionAsync(document, item, options, displayOptions, cancellationToken).ConfigureAwait(false);
            GC.KeepAlive(semanticModel);
            return description;
        }

        /// <summary>
        /// Gets the change to be applied when the item is committed.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="item">The item to get the change for.</param>
        /// <param name="commitCharacter">The typed character that caused the item to be committed. 
        /// This character may be used as part of the change. 
        /// This value is null when the commit was caused by the [TAB] or [ENTER] keys.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            char? commitCharacter = null,
            CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(item, document.Project);
            if (provider != null)
            {
                // We don't need SemanticModel here, just want to make sure it won't get GC'd before CompletionProviders are able to get it.
                (document, var semanticModel) = await GetDocumentWithFrozenPartialSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
                var change = await provider.GetChangeAsync(document, item, commitCharacter, cancellationToken).ConfigureAwait(false);
                GC.KeepAlive(semanticModel);
                return change;
            }
            else
            {
                return CompletionChange.Create(new TextChange(item.Span, item.DisplayText));
            }
        }

        /// <summary>
        /// Given a list of completion items that match the current code typed by the user,
        /// returns the item that is considered the best match, and whether or not that
        /// item should be selected or not.
        /// 
        /// itemToFilterText provides the values that each individual completion item should
        /// be filtered against.
        /// </summary>
        public virtual ImmutableArray<CompletionItem> FilterItems(
            Document document,
            ImmutableArray<CompletionItem> items,
            string filterText)
        {
            var helper = CompletionHelper.GetHelper(document);
            var itemsWithPatternMatch = new SegmentedList<(CompletionItem, PatternMatch?)>(items.Select(
                item => (item, helper.GetMatch(item, filterText, includeMatchSpans: false, CultureInfo.CurrentCulture))));

            var builder = ImmutableArray.CreateBuilder<CompletionItem>();
            FilterItems(helper, itemsWithPatternMatch, filterText, builder);
            return builder.ToImmutable();
        }

        internal virtual void FilterItems(
           Document document,
           IReadOnlyList<(CompletionItem, PatternMatch?)> itemsWithPatternMatch,
           string filterText,
           IList<CompletionItem> builder)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            // Default implementation just drops the pattern matches and builder, and calls the public overload of FilterItems instead for compatibility.
            builder.AddRange(FilterItems(document, itemsWithPatternMatch.SelectAsArray(item => item.Item1), filterText));
#pragma warning restore RS0030 // Do not used banned APIs
        }

        // The FilterItems method might need to handle a large list of items when import completion is enabled and filter text is
        // very short, i.e. <= 1. Therefore, use pooled list to avoid repeated (potentially LOH) allocations.
        private static readonly ObjectPool<List<(CompletionItem item, PatternMatch? match)>> s_listOfItemMatchPairPool = new(factory: () => new());

        /// <summary>
        /// Determine among the provided items the best match w.r.t. the given filter text, 
        /// those returned would be considered equally good candidates for selection by controller.
        /// </summary>
        internal static void FilterItems(
            CompletionHelper completionHelper,
            IReadOnlyList<(CompletionItem item, PatternMatch? match)> itemsWithPatternMatch,
            string filterText,
            IList<CompletionItem> builder)
        {
            // It's very common for people to type expecting completion to fix up their casing,
            // so if no uppercase characters were typed so far, we'd loosen our standard on comparing items
            // in case-sensitive manner and take into consideration the MatchPriority as well.
            // i.e. when everything else is equal, then if item1 is a better case-sensitive match but item2 has higher 
            // MatchPriority, we consider them equally good match, so the controller will later have a chance to
            // decide which is the best one to select.
            var filterTextContainsNoUpperLetters = true;
            for (var i = 0; i < filterText.Length; ++i)
            {
                if (char.IsUpper(filterText[i]))
                {
                    filterTextContainsNoUpperLetters = false;
                    break;
                }
            }

            // Keep track the highest MatchPriority of all items in the best list.
            var highestMatchPriorityInBest = int.MinValue;
            var bestItems = s_listOfItemMatchPairPool.Allocate();

            // This contains a list of items that are considered equally good match as bestItems except casing,
            // and they have higher MatchPriority than the ones in bestItems (although as a perf optimization we don't
            // actually guarantee this during the process, instead we check the MatchPriority again after the loop.)
            var itemsWithCasingMismatchButHigherMatchPriority = s_listOfItemMatchPairPool.Allocate();

            try
            {
                foreach (var pair in itemsWithPatternMatch)
                {
                    if (bestItems.Count == 0)
                    {
                        // We've found no good items yet.  So this is the best item currently.
                        bestItems.Add(pair);
                        highestMatchPriorityInBest = pair.item.Rules.MatchPriority;
                        continue;
                    }

                    var (bestItem, bestItemMatch) = bestItems[0];
                    var comparison = completionHelper.CompareItems(
                        pair.item, pair.match, bestItem, bestItemMatch, out var onlyDifferInCaseSensitivity);

                    if (comparison == 0)
                    {
                        // This item is as good as the items we've been collecting.  We'll return it and let the controller
                        // decide what to do.  (For example, it will pick the one that has the best MRU index).
                        // Also there's no need to remove items with lower MatchPriority from similarItemsWithHigerMatchPriority
                        // list, we will only add ones with higher value at the end.
                        bestItems.Add(pair);
                        highestMatchPriorityInBest = Math.Max(highestMatchPriorityInBest, pair.item.Rules.MatchPriority);
                    }
                    else if (comparison < 0)
                    {
                        // This item is strictly better than the best items we've found so far.

                        // Switch the references to the two lists to avoid potential of copying multiple elements around.
                        // Now itemsWithCasingMismatchButHigherMatchPriority contains prior best items.
                        (bestItems, itemsWithCasingMismatchButHigherMatchPriority) = (itemsWithCasingMismatchButHigherMatchPriority, bestItems);

                        // However, if this item only better in terms of case-sensitivity, and its MatchPriority is lower than prior best items,
                        // we'd like to save prior best items and consider their MatchPriority later. Otherwise, no need to keep track the prior best items.
                        if (!filterTextContainsNoUpperLetters ||
                            !onlyDifferInCaseSensitivity ||
                            highestMatchPriorityInBest <= pair.item.Rules.MatchPriority)
                        {
                            itemsWithCasingMismatchButHigherMatchPriority.Clear();
                        }

                        bestItems.Clear();
                        bestItems.Add(pair);
                        highestMatchPriorityInBest = pair.item.Rules.MatchPriority;
                    }
                    else
                    {
                        // This item is strictly worse than the ones we've been collecting.
                        // However, if it's only worse in terms of case-sensitivity, and has higher MatchPriority
                        // than all current best items, we'd like to save it and consider its MatchPriority later.
                        if (filterTextContainsNoUpperLetters &&
                            onlyDifferInCaseSensitivity &&
                            pair.item.Rules.MatchPriority > highestMatchPriorityInBest)
                        {
                            itemsWithCasingMismatchButHigherMatchPriority.Add(pair);
                        }
                    }
                }

                // Include those similar items (only worse in terms of case-sensitivity) that have better MatchPriority.
                foreach (var pair in itemsWithCasingMismatchButHigherMatchPriority)
                {
                    if (pair.item.Rules.MatchPriority > highestMatchPriorityInBest)
                    {
                        bestItems.Add(pair);
                    }
                }

                builder.AddRange(bestItems.Select(itemWithPatternMatch => itemWithPatternMatch.item));
            }
            finally
            {
                // Don't call ClearAndFree, which resets the capacity to a default value.
                bestItems.Clear();
                itemsWithCasingMismatchButHigherMatchPriority.Clear();
                s_listOfItemMatchPairPool.Free(bestItems);
                s_listOfItemMatchPairPool.Free(itemsWithCasingMismatchButHigherMatchPriority);
            }
        }

        /// <summary>
        /// Don't call. Used for pre-populating MEF providers only.
        /// </summary>
        internal IReadOnlyList<Lazy<CompletionProvider, CompletionProviderMetadata>> GetLazyImportedProviders()
            => _providerManager.GetLazyImportedProviders();

        /// <summary>
        /// Don't call. Used for pre-populating NuGet providers only.
        /// </summary>
        internal static ImmutableArray<CompletionProvider> GetProjectCompletionProviders(Project project)
            => ProviderManager.GetProjectCompletionProviders(project);

        internal CompletionProvider? GetProvider(CompletionItem item, Project? project)
            => _providerManager.GetProvider(item, project);

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionService _completionServiceWithProviders;

            public TestAccessor(CompletionService completionServiceWithProviders)
                => _completionServiceWithProviders = completionServiceWithProviders;

            internal ImmutableArray<CompletionProvider> GetAllProviders(ImmutableHashSet<string> roles, Project? project = null)
                => _completionServiceWithProviders._providerManager.GetTestAccessor().GetProviders(roles, project);

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

                return await CompletionService.GetContextAsync(
                    provider,
                    document,
                    position,
                    triggerInfo,
                    options,
                    defaultItemSpan,
                    sharedContext: null,
                    cancellationToken).ConfigureAwait(false);
            }

            public void SuppressPartialSemantics()
                => _completionServiceWithProviders._suppressPartialSemantics = true;
        }
    }
}
