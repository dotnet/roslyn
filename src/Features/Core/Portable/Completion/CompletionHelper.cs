// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionHelper
    {
        private static readonly CompletionHelper CaseSensitiveInstance = new CompletionHelper(isCaseSensitive: true);
        private static readonly CompletionHelper CaseInsensitiveInstance = new CompletionHelper(isCaseSensitive: false);

        private readonly object _gate = new object();
        private readonly Dictionary<CultureInfo, Dictionary<string, PatternMatcher>> _patternMatcherMap =
             new Dictionary<CultureInfo, Dictionary<string, PatternMatcher>>();
        private readonly Dictionary<CultureInfo, Dictionary<string, PatternMatcher>> _fallbackPatternMatcherMap =
            new Dictionary<CultureInfo, Dictionary<string, PatternMatcher>>();

        private static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");
        private readonly bool _isCaseSensitive;

        private CompletionHelper(bool isCaseSensitive)
        {
            _isCaseSensitive = isCaseSensitive;
        }

        public static CompletionHelper GetHelper(Workspace workspace, string language)
        {
            var isCaseSensitive = true;
            var ls = workspace.Services.GetLanguageServices(language);
            if (ls != null)
            {
                var syntaxFacts = ls.GetService<ISyntaxFactsService>();
                isCaseSensitive = syntaxFacts?.IsCaseSensitive ?? true;
            }

            return isCaseSensitive ? CaseSensitiveInstance : CaseInsensitiveInstance;
        }

        public static CompletionHelper GetHelper(Document document)
        {
            return GetHelper(document.Project.Solution.Workspace, document.Project.Language);
        }

        public IReadOnlyList<TextSpan> GetHighlightedSpans(
            CompletionItem completionItem, string filterText, CultureInfo culture)
        {
            var match = GetMatch(completionItem, filterText, includeMatchSpans: true, culture: culture);
            return match?.MatchedSpans;
        }

        /// <summary>
        /// Returns true if the completion item matches the filter text typed so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public bool MatchesFilterText(CompletionItem item, string filterText, CultureInfo culture)
        {
            return GetMatch(item, filterText, culture) != null;
        }

        private PatternMatch? GetMatch(CompletionItem item, string filterText, CultureInfo culture)
        {
            return GetMatch(item, filterText, includeMatchSpans: false, culture: culture);
        }

        private PatternMatch? GetMatch(
            CompletionItem item, string filterText,
            bool includeMatchSpans, CultureInfo culture)
        {
            // If the item has a dot in it (i.e. for something like enum completion), then attempt
            // to match what the user wrote against the last portion of the name.  That way if they
            // write "Bl" and we have "Blub" and "Color.Black", we'll consider hte latter to be a
            // better match as they'll both be prefix matches, and the latter will have a higher
            // priority.

            var lastDotIndex = item.FilterText.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                var textAfterLastDot = item.FilterText.Substring(lastDotIndex + 1);
                var match = GetMatchWorker(textAfterLastDot, filterText, includeMatchSpans, culture);
                if (match != null)
                {
                    return match;
                }
            }

            // Didn't have a dot, or the user text didn't match the portion after the dot.
            // Just do a normal check against the entire completion item.
            return GetMatchWorker(item.FilterText, filterText, includeMatchSpans, culture);
        }

        private PatternMatch? GetMatchWorker(
            string completionItemText, string filterText,
            bool includeMatchSpans, CultureInfo culture)
        {
            var patternMatcher = this.GetPatternMatcher(filterText, culture);
            var match = patternMatcher.GetFirstMatch(completionItemText, includeMatchSpans);

            if (match != null)
            {
                return match;
            }

            // Start with the culture-specific comparison, and fall back to en-US.
            if (!culture.Equals(EnUSCultureInfo))
            {
                patternMatcher = this.GetEnUSPatternMatcher(filterText);
                match = patternMatcher.GetFirstMatch(completionItemText);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private PatternMatcher GetPatternMatcher(
            string value, CultureInfo culture, Dictionary<CultureInfo, Dictionary<string, PatternMatcher>> map)
        {
            lock (_gate)
            {
                Dictionary<string, PatternMatcher> innerMap;
                if (!map.TryGetValue(culture, out innerMap))
                {
                    innerMap = new Dictionary<string, PatternMatcher>();
                    map[culture] = innerMap;
                }

                PatternMatcher patternMatcher;
                if (!innerMap.TryGetValue(value, out patternMatcher))
                {
                    patternMatcher = new PatternMatcher(value, culture,
                        verbatimIdentifierPrefixIsWordCharacter: true,
                        allowFuzzyMatching: false);
                    innerMap.Add(value, patternMatcher);
                }

                return patternMatcher;
            }
        }

        private PatternMatcher GetPatternMatcher(string value, CultureInfo culture)
        {
            return GetPatternMatcher(value, culture, _patternMatcherMap);
        }

        private PatternMatcher GetEnUSPatternMatcher(string value)
        {
            return GetPatternMatcher(value, EnUSCultureInfo, _fallbackPatternMatcherMap);
        }

        /// <summary>
        /// Returns true if item1 is a better completion item than item2 given the provided filter
        /// text, or false if it is not better.
        /// </summary>
        public int CompareItems(CompletionItem item1, CompletionItem item2, string filterText, CultureInfo culture)
        {
            var match1 = GetMatch(item1, filterText, culture);
            var match2 = GetMatch(item2, filterText, culture);

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

            // If they both seemed just as good, but they differ on preselection, then
            // item1 is better if it is preselected, otherwise it is worse.
            var diff = item1.Rules.MatchPriority - item2.Rules.MatchPriority;
            if (diff != 0)
            {
                return -diff;
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

            // Now, after comparing matches, check if an item wants to be preselected.  If so,
            // we prefer that.  i.e. say the user has typed 'f' and we have the items 'foo' 
            // and 'False' (with the latter being 'Preselected').  Both will be a prefix match.
            // And because we are ignoring case, neither will be seen as better.  Now, because
            // 'False' is preselected we pick it even though 'foo' matches 'f' case sensitively.
            diff = item2.Rules.MatchPriority - item1.Rules.MatchPriority;
            if (diff != 0)
            {
                return diff;
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
    }
}