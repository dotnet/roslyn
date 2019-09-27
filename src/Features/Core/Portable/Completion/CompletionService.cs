// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static CompletionService GetService(Document document)
            => document?.GetLanguageService<CompletionService>();

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets the current presentation and behavior rules.
        /// </summary>
        public virtual CompletionRules GetRules() => CompletionRules.Default;

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
            ImmutableHashSet<string> roles = null,
            OptionSet options = null)
        {
            return false;
        }

        /// <summary>
        /// Gets the span of the syntax element at the caret position.
        /// This is the most common value used for <see cref="CompletionItem.Span"/>.
        /// </summary>
        /// <param name="text">The document text that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret within the text.</param>
        [Obsolete("Not used anymore. CompletionService.GetDefaultCompletionListSpan is used instead.", error: true)]
        public virtual TextSpan GetDefaultItemSpan(SourceText text, int caretPosition)
        {
            return GetDefaultCompletionListSpan(text, caretPosition);
        }

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
        public abstract Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger = default,
            ImmutableHashSet<string> roles = null,
            OptionSet options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the completions available at the caret position, with additional info indicates 
        /// whether expander items are available.
        /// </summary>
        /// <remarks>
        /// expandItemsAvailable is true when expanded items are returned or can be provided upon request.
        /// </remarks>
        internal virtual async Task<(CompletionList completionList, bool expandItemsAvailable)> GetCompletionsInternalAsync(
             Document document,
             int caretPosition,
             CompletionTrigger trigger = default,
             ImmutableHashSet<string> roles = null,
             OptionSet options = null,
             CancellationToken cancellationToken = default)
        {
            var completionList = await GetCompletionsAsync(document, caretPosition, trigger, roles, options, cancellationToken).ConfigureAwait(false);
            return (completionList, false);
        }

        /// <summary>
        /// Gets the description of the item.
        /// </summary>
        /// <param name="document">This will be the  original document that
        /// <paramref name="item"/> was created against.</param>
        /// <param name="item">The item to get the description for.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionDescription> GetDescriptionAsync(
            Document document,
            CompletionItem item,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompletionDescription.Empty);
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
        public virtual Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            char? commitCharacter = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, item.DisplayText)));
        }

        /// <summary>
        /// Preferred overload of <see cref="GetChangeAsync(Document, CompletionItem, char?,
        /// CancellationToken)"/>.
        ///
        /// This overload is passed the value of <see cref="CompletionContext.CompletionListSpan"/>
        /// which should be used to determine what span should be updated in the original <paramref
        /// name="document"/> passed in.
        /// </summary>
        internal virtual Task<CompletionChange> GetChangeAsync(
            Document document, CompletionItem item, TextSpan completionListSpan,
            char? commitCharacter = null, CancellationToken cancellationToken = default)
        {
            return GetChangeAsync(document, item, commitCharacter, cancellationToken);
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

            return FilterItems(completionHelper, itemsWithPatternMatch);
        }

        internal static ImmutableArray<CompletionItem> FilterItems(
            CompletionHelper completionHelper,
            ImmutableArray<(CompletionItem item, PatternMatch? match)> itemsWithPatternMatch)
        {
            var bestItems = ArrayBuilder<(CompletionItem, PatternMatch?)>.GetInstance();
            foreach (var pair in itemsWithPatternMatch)
            {
                if (bestItems.Count == 0)
                {
                    // We've found no good items yet.  So this is the best item currently.
                    bestItems.Add(pair);
                }
                else
                {
                    var (bestItem, bestItemMatch) = bestItems.First();
                    var comparison = completionHelper.CompareItems(pair.item, pair.match, bestItem, bestItemMatch);
                    if (comparison < 0)
                    {
                        // This item is strictly better than the best items we've found so far.
                        bestItems.Clear();
                        bestItems.Add(pair);
                    }
                    else if (comparison == 0)
                    {
                        // This item is as good as the items we've been collecting.  We'll return 
                        // it and let the controller decide what to do.  (For example, it will
                        // pick the one that has the best MRU index).
                        bestItems.Add(pair);
                    }
                    // otherwise, this item is strictly worse than the ones we've been collecting.
                    // We can just ignore it.
                }
            }

            return bestItems.ToImmutableAndFree().SelectAsArray(itemWithPatternMatch => itemWithPatternMatch.Item1);
        }
    }
}
