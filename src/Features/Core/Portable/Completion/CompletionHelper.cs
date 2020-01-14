// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionHelper
    {
        private readonly object _gate = new object();
        private readonly Dictionary<(string pattern, CultureInfo, bool includeMatchedSpans), PatternMatcher> _patternMatcherMap =
             new Dictionary<(string pattern, CultureInfo, bool includeMatchedSpans), PatternMatcher>();

        private static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");
        private readonly bool _isCaseSensitive;

        public CompletionHelper(bool isCaseSensitive)
        {
            _isCaseSensitive = isCaseSensitive;
        }

        public static CompletionHelper GetHelper(Document document)
        {
            return document.Project.Solution.Workspace.Services.GetRequiredService<ICompletionHelperService>()
                .GetCompletionHelper(document);
        }

        public ImmutableArray<TextSpan> GetHighlightedSpans(
                string text, string pattern, CultureInfo culture)
        {
            var match = GetMatch(text, pattern, includeMatchSpans: true, culture: culture);
            return match == null ? ImmutableArray<TextSpan>.Empty : match.Value.MatchedSpans;
        }

        /// <summary>
        /// Returns true if the completion item matches the pattern so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public bool MatchesPattern(string text, string pattern, CultureInfo culture)
            => GetMatch(text, pattern, includeMatchSpans: false, culture) != null;

        public PatternMatch? GetMatch(
            string completionItemText,
            string pattern,
            bool includeMatchSpans,
            CultureInfo culture)
        {
            // If the item has a dot in it (i.e. for something like enum completion), then attempt
            // to match what the user wrote against the last portion of the name.  That way if they
            // write "Bl" and we have "Blub" and "Color.Black", we'll consider the latter to be a
            // better match as they'll both be prefix matches, and the latter will have a higher
            // priority.

            var lastDotIndex = completionItemText.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                var afterDotPosition = lastDotIndex + 1;
                var textAfterLastDot = completionItemText.Substring(afterDotPosition);

                var match = GetMatchWorker(textAfterLastDot, pattern, culture, includeMatchSpans);
                if (match != null)
                {
                    return AdjustMatchedSpans(match.Value, afterDotPosition);
                }
            }

            // Didn't have a dot, or the user text didn't match the portion after the dot.
            // Just do a normal check against the entire completion item.
            return GetMatchWorker(completionItemText, pattern, culture, includeMatchSpans);
        }

        private PatternMatch? AdjustMatchedSpans(PatternMatch value, int offset)
            => value.MatchedSpans.IsDefaultOrEmpty
                ? value
                : value.WithMatchedSpans(value.MatchedSpans.SelectAsArray(s => new TextSpan(s.Start + offset, s.Length)));

        private PatternMatch? GetMatchWorker(
            string completionItemText, string pattern,
            CultureInfo culture, bool includeMatchSpans)
        {
            var patternMatcher = GetPatternMatcher(pattern, culture, includeMatchSpans);
            var match = patternMatcher.GetFirstMatch(completionItemText);

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
            patternMatcher = GetPatternMatcher(pattern, EnUSCultureInfo, includeMatchSpans);
            var enUSCultureMatch = patternMatcher.GetFirstMatch(completionItemText);

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

        private PatternMatcher GetPatternMatcher(
            string pattern, CultureInfo culture, bool includeMatchedSpans,
            Dictionary<(string, CultureInfo, bool), PatternMatcher> map)
        {
            lock (_gate)
            {
                var key = (pattern, culture, includeMatchedSpans);
                if (!map.TryGetValue(key, out var patternMatcher))
                {
                    patternMatcher = PatternMatcher.CreatePatternMatcher(
                        pattern, culture, includeMatchedSpans,
                        allowFuzzyMatching: false);
                    map.Add(key, patternMatcher);
                }

                return patternMatcher;
            }
        }

        private PatternMatcher GetPatternMatcher(string pattern, CultureInfo culture, bool includeMatchedSpans)
            => GetPatternMatcher(pattern, culture, includeMatchedSpans, _patternMatcherMap);

        /// <summary>
        /// Returns true if item1 is a better completion item than item2 given the provided filter
        /// text, or false if it is not better.
        /// </summary>
        public int CompareItems(CompletionItem item1, CompletionItem item2, string pattern, CultureInfo culture)
        {
            var match1 = GetMatch(item1.FilterText, pattern, includeMatchSpans: false, culture);
            var match2 = GetMatch(item2.FilterText, pattern, includeMatchSpans: false, culture);

            return CompareItems(item1, match1, item2, match2);
        }

        public int CompareItems(CompletionItem item1, PatternMatch? match1, CompletionItem item2, PatternMatch? match2)
        {
            if (match1 != null && match2 != null)
            {
                var result = CompareMatches(match1.Value, match2.Value, item1, item2);
                if (result != 0)
                {
                    return result;
                }
            }
            else if (match1 != null)
            {
                return -1;
            }
            else if (match2 != null)
            {
                return 1;
            }

            var preselectionDiff = ComparePreselection(item1, item2);
            if (preselectionDiff != 0)
            {
                return preselectionDiff;
            }

            // Prefer things with a keyword tag, if the filter texts are the same.
            if (!TagsEqual(item1, item2) && item1.FilterText == item2.FilterText)
            {
                return (!IsKeywordItem(item1)).CompareTo(!IsKeywordItem(item2));
            }

            return 0;
        }

        private static bool TagsEqual(CompletionItem item1, CompletionItem item2)
            => TagsEqual(item1.Tags, item2.Tags);

        private static bool TagsEqual(ImmutableArray<string> tags1, ImmutableArray<string> tags2)
            => tags1 == tags2 || System.Linq.Enumerable.SequenceEqual(tags1, tags2);

        private static bool IsKeywordItem(CompletionItem item)
            => item.Tags.Contains(WellKnownTags.Keyword);

        private int CompareMatches(PatternMatch match1, PatternMatch match2, CompletionItem item1, CompletionItem item2)
        {
            // Always prefer non-expanded item regardless of the pattern matching result.
            // This currently means unimported types will be treated as "2nd tier" results,
            // which forces users to be more explicit about selecting them.
            var expandedDiff = CompareExpandedItem(item1, match1, item2, match2);
            if (expandedDiff != 0)
            {
                return expandedDiff;
            }

            // Then see how the two items compare in a case insensitive fashion.  Matches that 
            // are strictly better (ignoring case) should prioritize the item.  i.e. if we have
            // a prefix match, that should always be better than a substring match.
            //
            // The reason we ignore case is that it's very common for people to type expecting
            // completion to fix up their casing.  i.e. 'false' will be written with the 
            // expectation that it will get fixed by the completion list to 'False'.  
            var diff = match1.CompareTo(match2, ignoreCase: true);
            if (diff != 0)
            {
                return diff;
            }

            var preselectionDiff = ComparePreselection(item1, item2);
            if (preselectionDiff != 0)
            {
                return preselectionDiff;
            }

            // At this point we have two items which we're matching in a rather similar fashion.
            // If one is a prefix of the other, prefer the prefix.  i.e. if we have 
            // "Table" and "table:=" and the user types 't' and we are in a case insensitive 
            // language, then we prefer the former.
            if (item1.GetEntireDisplayText().Length != item2.GetEntireDisplayText().Length)
            {
                var comparison = _isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (item2.GetEntireDisplayText().StartsWith(item1.GetEntireDisplayText(), comparison))
                {
                    return -1;
                }
                else if (item1.GetEntireDisplayText().StartsWith(item2.GetEntireDisplayText(), comparison))
                {
                    return 1;
                }
            }

            // Now compare the matches again in a case sensitive manner.  If everything was
            // equal up to this point, we prefer the item that better matches based on case.
            return match1.CompareTo(match2, ignoreCase: false);
        }

        // If they both seemed just as good, but they differ on preselection, then
        // item1 is better if it is preselected, otherwise it is worse.
        private int ComparePreselection(CompletionItem item1, CompletionItem item2)
            => (item1.Rules.MatchPriority != MatchPriority.Preselect).CompareTo(item2.Rules.MatchPriority != MatchPriority.Preselect);

        private static int CompareExpandedItem(CompletionItem item1, PatternMatch match1, CompletionItem item2, PatternMatch match2)
        {
            var isItem1Expanded = item1.Flags.IsExpanded();
            var isItem2Expanded = item2.Flags.IsExpanded();

            if (isItem1Expanded == isItem2Expanded)
            {
                return 0;
            }

            var isItem1ExactMatch = match1.Kind == PatternMatchKind.Exact;
            var isItem2ExactMatch = match2.Kind == PatternMatchKind.Exact;

            // If neither of the items is an exact match, or both are exact matches,
            // then we prefer non-expanded item over expanded one.
            // 
            // For example, suppose we have two types `Namespace1.Cafe` and `Namespace2.Cafe`, and import completion is enabled.
            // In the scenarios below, `Namespace1.Cafe` would be selected over `Namespace2.Cafe`

            //  using Namespace1;
            //  class C
            //  {
            //      cafe$$
            //  }

            //  using Namespace1;
            //  class C
            //  {
            //      caf$$
            //  }

            if (!isItem1ExactMatch && !isItem2ExactMatch
                || match1.Kind == match2.Kind)
            {
                return isItem1Expanded ? 1 : -1;
            }

            // We prefer expanded item over non-expanded one iff the expanded item 
            // is an exact match whereas the non-expanded one isn't.
            // 
            // For example, suppose we have two types `Namespace1.Cafe1` and `Namespace2.Cafe`, and import completion is enabled.
            // In the scenarios below, `Namespace2.Cafe` would be selected over `Namespace1.Cafe1`

            //  using Namespace1;
            //  class C
            //  {
            //      cafe$$
            //  }
            if (isItem1Expanded && isItem1ExactMatch)
            {
                return -1;
            }
            else if (isItem2Expanded && isItem2ExactMatch)
            {
                return 1;
            }

            // Non-expanded item is the only exact match, so we definitely prefer it.
            return isItem1Expanded ? 1 : -1;
        }

        public static string ConcatNamespace(string? containingNamespace, string name)
            => string.IsNullOrEmpty(containingNamespace) ? name : containingNamespace + "." + name;
    }
}
