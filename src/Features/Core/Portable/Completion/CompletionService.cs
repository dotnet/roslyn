// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A per language service for constructing context dependent list of completions that 
    /// can be presented to a user during typing in an editor.
    /// </summary>
    public abstract class CompletionService : ILanguageService
    {
        // Prevent inheritance outside of Roslyn.
        internal CompletionService()
        {
        }

        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static CompletionService? GetService(Document? document)
            => document?.GetLanguageService<CompletionService>();

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets the current presentation and behavior rules.
        /// </summary>
        public virtual CompletionRules GetRules()
            => CompletionRules.Default;

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
        public virtual bool ShouldTriggerCompletion(
            SourceText text,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles = null,
            OptionSet? options = null)
        {
            return false;
        }

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
            HostLanguageServices languageServices,
            SourceText text,
            int caretPosition,
            CompletionTrigger trigger,
            CompletionOptions options,
            OptionSet passThroughOptions,
            ImmutableHashSet<string>? roles = null)
        {
            Debug.Fail("Backward compat only, should not be called");
            return ShouldTriggerCompletion(text, caretPosition, trigger, roles, passThroughOptions);
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
                text, caretPosition, c => char.IsLetter(c), c => char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// Gets the completions available at the caret position.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <param name="cancellationToken"></param>
        public abstract Task<CompletionList?> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger = default,
            ImmutableHashSet<string>? roles = null,
            OptionSet? options = null,
            CancellationToken cancellationToken = default);

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
#pragma warning disable RS0030 // Do not use banned APIs
            return await GetCompletionsAsync(document, caretPosition, trigger, roles, passThroughOptions, cancellationToken).ConfigureAwait(false) ?? CompletionList.Empty;
#pragma warning restore
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
            Debug.Fail("For backwards API compat only, should not be called");

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
        internal abstract Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the change to be applied when the item is committed.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="item">The item to get the change for.</param>
        /// <param name="commitCharacter">The typed character that caused the item to be committed. 
        /// This character may be used as part of the change. 
        /// This value is null when the commit was caused by the [TAB] or [ENTER] keys.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            char? commitCharacter = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, item.DisplayText)));
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
            return FilterItems(helper, items, filterText);
        }

        internal virtual ImmutableArray<CompletionItem> FilterItems(
           Document document,
           ImmutableArray<(CompletionItem, PatternMatch?)> itemsWithPatternMatch,
           string filterText)
        {
            // Default implementation just drops the pattern matches and
            // calls the public overload of FilterItems for compatibility.
            return FilterItems(document, itemsWithPatternMatch.SelectAsArray(item => item.Item1), filterText);
        }

        internal static ImmutableArray<CompletionItem> FilterItems(
            CompletionHelper completionHelper,
            ImmutableArray<CompletionItem> items,
            string filterText)
        {
            var itemsWithPatternMatch = items.SelectAsArray(
                item => (item, completionHelper.GetMatch(item.FilterText, filterText, includeMatchSpans: false, CultureInfo.CurrentCulture)));

            return FilterItems(completionHelper, itemsWithPatternMatch, filterText);
        }

        /// <summary>
        /// Determine among the provided items the best match w.r.t. the given filter text, 
        /// those returned would be considered equally good candidates for selection by controller.
        /// </summary>
        internal static ImmutableArray<CompletionItem> FilterItems(
            CompletionHelper completionHelper,
            ImmutableArray<(CompletionItem item, PatternMatch? match)> itemsWithPatternMatch,
            string filterText)
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
            using var _1 = ArrayBuilder<(CompletionItem item, PatternMatch? match)>.GetInstance(out var bestItems);

            // This contains a list of items that are considered equally good match as bestItems except casing,
            // and they have higher MatchPriority than the ones in bestItems (although as a perf optimization we don't
            // actually guarantee this during the process, instead we check the MatchPriority again after the loop.)
            using var _2 = ArrayBuilder<(CompletionItem item, PatternMatch? match)>.GetInstance(out var itemsWithCasingMismatchButHigherMatchPriority);

            foreach (var pair in itemsWithPatternMatch)
            {
                if (bestItems.Count == 0)
                {
                    // We've found no good items yet.  So this is the best item currently.
                    bestItems.Add(pair);
                    highestMatchPriorityInBest = pair.item.Rules.MatchPriority;
                    continue;
                }

                var (bestItem, bestItemMatch) = bestItems.First();
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
                    // However, if it's only better in terms of case-sensitivity, we'd like 
                    // to save the prior best items and consider their MatchPriority later.
                    itemsWithCasingMismatchButHigherMatchPriority.Clear();

                    if (filterTextContainsNoUpperLetters &&
                        onlyDifferInCaseSensitivity &&
                        highestMatchPriorityInBest > pair.item.Rules.MatchPriority) // don't add if this item has higher MatchPriority than all prior best items
                    {
                        itemsWithCasingMismatchButHigherMatchPriority.AddRange(bestItems);
                    }

                    bestItems.Clear();
                    bestItems.Add(pair);
                    highestMatchPriorityInBest = pair.item.Rules.MatchPriority;
                }
                else
                {
                    // otherwise, this item is strictly worse than the ones we've been collecting.
                    // However, if it's only worse in terms of case-sensitivity, we'd like 
                    // to save it and consider its MatchPriority later.
                    if (filterTextContainsNoUpperLetters &&
                        onlyDifferInCaseSensitivity &&
                        pair.item.Rules.MatchPriority > highestMatchPriorityInBest)  // don't add if this item doesn't have higher MatchPriority
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

            return bestItems.ToImmutable().SelectAsArray(itemWithPatternMatch => itemWithPatternMatch.item);
        }
    }
}
