﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PatternMatching;
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

        // Support for completion items with extra decorative characters in their DisplayText.
        // This allows bolding and MRU to operate on the "real" display text (without text
        // decorations). This should be a substring of the corresponding DisplayText.
        private static string DisplayTextForMatching = nameof(DisplayTextForMatching);

        public CompletionHelper(bool isCaseSensitive)
        {
            _isCaseSensitive = isCaseSensitive;
        }

        public static CompletionHelper GetHelper(Document document)
        {
            return document.Project.Solution.Workspace.Services.GetService<ICompletionHelperService>()
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
            => GetMatch(text, pattern, culture) != null;

        private PatternMatch? GetMatch(string text, string pattern, CultureInfo culture)
            => GetMatch(text, pattern, includeMatchSpans: false, culture: culture);

        private PatternMatch? GetMatch(
            string completionItemText, string pattern,
            bool includeMatchSpans, CultureInfo culture)
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
            var patternMatcher = this.GetPatternMatcher(pattern, culture, includeMatchSpans);
            var match = patternMatcher.GetFirstMatch(completionItemText);

            if (match != null)
            {
                return match;
            }

            // Start with the culture-specific comparison, and fall back to en-US.
            if (!culture.Equals(EnUSCultureInfo))
            {
                patternMatcher = this.GetPatternMatcher(pattern, EnUSCultureInfo, includeMatchSpans);
                match = patternMatcher.GetFirstMatch(completionItemText);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
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
            var match1 = GetMatch(item1.FilterText, pattern, culture);
            var match2 = GetMatch(item2.FilterText, pattern, culture);

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
                return IsKeywordItem(item1) ? -1 : IsKeywordItem(item2) ? 1 : 0;
            }

            return 0;
        }

        private static bool TagsEqual(CompletionItem item1, CompletionItem item2)
        {
            return TagsEqual(item1.Tags, item2.Tags);
        }

        private static bool TagsEqual(ImmutableArray<string> tags1, ImmutableArray<string> tags2)
        {
            return tags1 == tags2 || System.Linq.Enumerable.SequenceEqual(tags1, tags2);
        }

        private static bool IsKeywordItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Keyword);
        }

        private int CompareMatches(PatternMatch match1, PatternMatch match2, CompletionItem item1, CompletionItem item2)
        {
            // First see how the two items compare in a case insensitive fashion.  Matches that 
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

            // At this point we have two items which we're matching in a rather similar fasion.
            // If one is a prefix of the other, prefer the prefix.  i.e. if we have 
            // "Table" and "table:=" and the user types 't' and we are in a case insensitive 
            // language, then we prefer the former.
            if (item1.DisplayText.Length != item2.DisplayText.Length)
            {
                var comparison = _isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (item2.DisplayText.StartsWith(item1.DisplayText, comparison))
                {
                    return -1;
                }
                else if (item1.DisplayText.StartsWith(item2.DisplayText, comparison))
                {
                    return 1;
                }
            }

            // Now compare the matches again in a case sensitive manner.  If everything was
            // equal up to this point, we prefer the item that better matches based on case.
            diff = match1.CompareTo(match2, ignoreCase: false);
            if (diff != 0)
            {
                return diff;
            }

            return 0;
        }

        private int ComparePreselection(CompletionItem item1, CompletionItem item2)
        {
            // If they both seemed just as good, but they differ on preselection, then
            // item1 is better if it is preselected, otherwise it is worse.
            if (item1.Rules.MatchPriority == MatchPriority.Preselect &&
                item2.Rules.MatchPriority != MatchPriority.Preselect)
            {
                return -1;
            }
            else if (item1.Rules.MatchPriority != MatchPriority.Preselect &&
                     item2.Rules.MatchPriority == MatchPriority.Preselect)
            {
                return 1;
            }

            return 0;
        }

        internal static string GetDisplayTextForMatching(CompletionItem item)
            => item.Properties.TryGetValue(DisplayTextForMatching, out var displayText) ? displayText : item.DisplayText;
    }
}
