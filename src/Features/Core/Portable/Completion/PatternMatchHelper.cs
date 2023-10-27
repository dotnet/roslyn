// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// This type is not thread safe due to the restriction of underlying PatternMatcher. 
    /// Must be disposed after use.
    /// </summary>
    internal sealed class PatternMatchHelper(string pattern) : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<(CultureInfo, bool includeMatchedSpans), PatternMatcher> _patternMatcherMap = new();

        private static readonly CultureInfo EnUSCultureInfo = new("en-US");

        public string Pattern { get; } = pattern;

        public ImmutableArray<TextSpan> GetHighlightedSpans(string text, CultureInfo culture)
        {
            var match = GetMatch(text, includeMatchSpans: true, culture: culture);
            return match == null ? ImmutableArray<TextSpan>.Empty : match.Value.MatchedSpans;
        }

        public PatternMatch? GetMatch(string text, bool includeMatchSpans, CultureInfo culture)
        {
            var patternMatcher = GetPatternMatcher(culture, includeMatchSpans);
            var match = patternMatcher.GetFirstMatch(text);

            // We still have making checks for language having different to English capitalization,
            // for example, for Turkish with dotted and dotless i capitalization totally diferent from English.
            // Now we escaping from the second check for English languages.
            // Maybe we can escape as well for more similar languages in case if we meet performance issues.
            if (culture.ThreeLetterWindowsLanguageName.Equals(EnUSCultureInfo.ThreeLetterWindowsLanguageName))
            {
                return match;
            }

            // Keywords in .NET are always in En-US.
            // Identifiers can be in user language.
            // Try to get matches for both and return the best of them.
            patternMatcher = GetPatternMatcher(EnUSCultureInfo, includeMatchSpans);
            var enUSCultureMatch = patternMatcher.GetFirstMatch(text);

            if (match == null)
            {
                return enUSCultureMatch;
            }

            if (enUSCultureMatch == null)
            {
                return match;
            }

            return match.Value.CompareTo(enUSCultureMatch.Value) < 0 ? match.Value : enUSCultureMatch.Value;
        }

        private PatternMatcher GetPatternMatcher(CultureInfo culture, bool includeMatchedSpans)
        {
            lock (_gate)
            {
                var key = (culture, includeMatchedSpans);
                if (!_patternMatcherMap.TryGetValue(key, out var patternMatcher))
                {
                    patternMatcher = PatternMatcher.CreatePatternMatcher(
                        Pattern, culture, includeMatchedSpans,
                        allowFuzzyMatching: false);
                    _patternMatcherMap.Add(key, patternMatcher);
                }

                return patternMatcher;
            }
        }

        public MatchResult GetMatchResult(
            CompletionItem item,
            bool includeMatchSpans,
            CultureInfo culture)
        {
            var match = GetMatch(item.FilterText, includeMatchSpans, culture);
            string? matchedAdditionalFilterText = null;

            if (item.HasAdditionalFilterTexts)
            {
                foreach (var additionalFilterText in item.AdditionalFilterTexts)
                {
                    var additionalMatch = GetMatch(additionalFilterText, includeMatchSpans, culture);
                    if (additionalMatch.HasValue && additionalMatch.Value.CompareTo(match, ignoreCase: false) < 0)
                    {
                        match = additionalMatch;
                        matchedAdditionalFilterText = additionalFilterText;
                    }
                }
            }

            return new MatchResult(
                item,
                shouldBeConsideredMatchingFilterText: match is not null,
                match,
                index: -1,
                matchedAdditionalFilterText);
        }

        /// <summary>
        /// Returns true if the completion item matches the pattern so far.  Returns 'true'
        /// if and only if the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public bool MatchesPattern(CompletionItem item, CultureInfo culture)
            => GetMatchResult(item, includeMatchSpans: false, culture).ShouldBeConsideredMatchingFilterText;

        public bool TryCreateMatchResult(
            CompletionItem item,
            CompletionTriggerKind initialTriggerKind,
            CompletionFilterReason filterReason,
            int recentItemIndex,
            bool includeMatchSpans,
            int currentIndex,
            out MatchResult matchResult)
        {
            // Get the match of the given completion item for the pattern provided so far. 
            // A completion item is checked against the pattern by see if it's 
            // CompletionItem.FilterText matches the item. That way, the pattern it checked 
            // against terms like "IList" and not IList<>.
            // Note that the check on filter text length is purely for efficiency, we should 
            // get the same result with or without it.
            var patternMatch = Pattern.Length > 0
                ? GetMatch(item.FilterText, includeMatchSpans, CultureInfo.CurrentCulture)
                : null;

            string? matchedAdditionalFilterText = null;
            var shouldBeConsideredMatchingFilterText = ShouldBeConsideredMatchingFilterText(
                item.FilterText,
                item.Rules.MatchPriority,
                initialTriggerKind,
                filterReason,
                recentItemIndex,
                patternMatch);

            if (Pattern.Length > 0 && item.HasAdditionalFilterTexts)
            {
                foreach (var additionalFilterText in item.AdditionalFilterTexts)
                {
                    var additionalMatch = GetMatch(additionalFilterText, includeMatchSpans, CultureInfo.CurrentCulture);
                    var additionalFlag = ShouldBeConsideredMatchingFilterText(
                        additionalFilterText,
                        item.Rules.MatchPriority,
                        initialTriggerKind,
                        filterReason,
                        recentItemIndex,
                        additionalMatch);

                    if (!shouldBeConsideredMatchingFilterText ||
                        additionalFlag && additionalMatch.HasValue && additionalMatch.Value.CompareTo(patternMatch, ignoreCase: false) < 0)
                    {
                        matchedAdditionalFilterText = additionalFilterText;
                        shouldBeConsideredMatchingFilterText = additionalFlag;
                        patternMatch = additionalMatch;
                    }
                }
            }

            if (shouldBeConsideredMatchingFilterText || KeepAllItemsInTheList(initialTriggerKind, Pattern))
            {
                matchResult = new MatchResult(
                    item, shouldBeConsideredMatchingFilterText,
                    patternMatch, currentIndex, matchedAdditionalFilterText, recentItemIndex);

                return true;
            }

            matchResult = default;
            return false;

            bool ShouldBeConsideredMatchingFilterText(
                string filterText,
                int matchPriority,
                CompletionTriggerKind initialTriggerKind,
                CompletionFilterReason filterReason,
                int recentItemIndex,
                PatternMatch? patternMatch)
            {
                // For the deletion we bake in the core logic for how matching should work.
                // This way deletion feels the same across all languages that opt into deletion 
                // as a completion trigger.

                // Specifically, to avoid being too aggressive when matching an item during 
                // completion, we require that the current filter text be a prefix of the 
                // item in the list.
                if (filterReason == CompletionFilterReason.Deletion &&
                    initialTriggerKind == CompletionTriggerKind.Deletion)
                {
                    return filterText.GetCaseInsensitivePrefixLength(Pattern) > 0;
                }

                // If the user hasn't typed anything, and this item was preselected, or was in the
                // MRU list, then we definitely want to include it.
                if (Pattern.Length == 0)
                {
                    if (recentItemIndex >= 0 || matchPriority > MatchPriority.Default)
                        return true;
                }

                // Otherwise, the item matches filter text if a pattern match is returned.
                return patternMatch != null;
            }

            // If the item didn't match the filter text, we still keep it in the list
            // if one of two things is true:
            //  1. The user has typed nothing or only typed a single character.  In this case they might
            //     have just typed the character to get completion.  Filtering out items
            //     here is not desirable.
            //
            //  2. They brought up completion with ctrl-j or through deletion.  In these
            //     cases we just always keep all the items in the list.
            static bool KeepAllItemsInTheList(CompletionTriggerKind initialTriggerKind, string filterText)
            {
                return filterText.Length <= 1 ||
                    initialTriggerKind == CompletionTriggerKind.Invoke ||
                    initialTriggerKind == CompletionTriggerKind.Deletion;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                foreach (var matcher in _patternMatcherMap.Values)
                    matcher.Dispose();

                _patternMatcherMap.Clear();
            }
        }
    }
}
