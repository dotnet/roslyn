﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionHelper
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, PatternMatcher> _patternMatcherMap = new Dictionary<string, PatternMatcher>();
        private readonly Dictionary<string, PatternMatcher> _fallbackPatternMatcherMap = new Dictionary<string, PatternMatcher>();
        private static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");
        private readonly bool _isCaseSensitive;

        protected CompletionHelper(bool isCaseSensitive)
        {
            _isCaseSensitive = isCaseSensitive;
        }

        public static CompletionHelper GetHelper(Workspace workspace, string language)
        {
            var ls = workspace.Services.GetLanguageServices(language);
            if (ls != null)
            {
                var factory = ls.GetService<CompletionHelperFactory>();
                if (factory != null)
                {
                    return factory.CreateCompletionHelper();
                }

                var syntaxFacts = ls.GetService<ISyntaxFactsService>();
                return new CompletionHelper(syntaxFacts?.IsCaseSensitive ?? true);
            }

            return null;
        }

        public static CompletionHelper GetHelper(Document document)
        {
            return GetHelper(document.Project.Solution.Workspace, document.Project.Language);
        }

        public IReadOnlyList<TextSpan> GetHighlightedSpans(CompletionItem completionItem, string filterText)
        {
            var match = GetMatch(completionItem, filterText, includeMatchSpans: true);
            return match?.MatchedSpans;
        }

        /// <summary>
        /// Returns true if the completion item matches the filter text typed so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public virtual bool MatchesFilterText(
            CompletionItem item, string filterText,
            CompletionTrigger trigger, ImmutableArray<string> recentItems)
        {
            // If the user hasn't typed anything, and this item was preselected, or was in the
            // MRU list, then we definitely want to include it.
            if (filterText.Length == 0)
            {
                if (item.Rules.MatchPriority > MatchPriority.Default || (!recentItems.IsDefault && GetRecentItemIndex(recentItems, item) < 0))
                {
                    return true;
                }
            }

            if (filterText.Length > 0 && IsAllDigits(filterText))
            {
                // The user is just typing a number.  We never want this to match against
                // anything we would put in a completion list.
                return false;
            }

            return GetMatch(item, filterText) != null;
        }

        private static int GetRecentItemIndex(ImmutableArray<string> recentItems, CompletionItem item)
        {
            var index = recentItems.IndexOf(item.DisplayText);
            return -index;
        }

        private static bool IsAllDigits(string filterText)
        {
            for (int i = 0; i < filterText.Length; i++)
            {
                if (filterText[i] < '0' || filterText[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private PatternMatch? GetMatch(CompletionItem item, string filterText)
        {
            return GetMatch(item, filterText, includeMatchSpans: false);
        }

        private PatternMatch? GetMatch(CompletionItem item, string filterText, bool includeMatchSpans)
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
                var match = GetMatchWorker(textAfterLastDot, filterText, includeMatchSpans);
                if (match != null)
                {
                    return match;
                }
            }

            // Didn't have a dot, or the user text didn't match the portion after the dot.
            // Just do a normal check against the entire completion item.
            return GetMatchWorker(item.FilterText, filterText, includeMatchSpans);
        }

        private PatternMatch? GetMatchWorker(string completionItemText, string filterText, bool includeMatchSpans)
        {
            var patternMatcher = this.GetPatternMatcher(filterText, CultureInfo.CurrentCulture);
            var match = patternMatcher.GetFirstMatch(completionItemText, includeMatchSpans);

            if (match != null)
            {
                return match;
            }

            // Start with the culture-specific comparison, and fall back to en-US.
            if (!CultureInfo.CurrentCulture.Equals(EnUSCultureInfo))
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
            string value, CultureInfo culture, Dictionary<string, PatternMatcher> map)
        {
            lock (_gate)
            {
                PatternMatcher patternMatcher;
                if (!map.TryGetValue(value, out patternMatcher))
                {
                    patternMatcher = new PatternMatcher(value, culture,
                        verbatimIdentifierPrefixIsWordCharacter: true,
                        allowFuzzyMatching: false);
                    map.Add(value, patternMatcher);
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
        public virtual bool IsBetterFilterMatch(CompletionItem item1, CompletionItem item2, string filterText, CompletionTrigger trigger, ImmutableArray<string> recentItems)
        {
            return IsBetterFilterMatchWorker(item1, item2, filterText, recentItems);
        }

        protected bool IsBetterFilterMatchWorker(CompletionItem item1, CompletionItem item2, string filterText, ImmutableArray<string> recentItems)
        {
            var match1 = GetMatch(item1, filterText);
            var match2 = GetMatch(item2, filterText);

            if (match1 != null && match2 != null)
            {
                var result = CompareMatches(match1.Value, match2.Value, item1, item2);
                if (result != 0)
                {
                    return result < 0;
                }
            }
            else if (match1 != null)
            {
                return true;
            }
            else if (match2 != null)
            {
                return false;
            }

            // If they both seemed just as good, but they differ on preselection, then
            // item1 is better if it is preselected, otherwise it is worse.
            if (item1.Rules.MatchPriority != item2.Rules.MatchPriority)
            {
                return item1.Rules.MatchPriority > item2.Rules.MatchPriority;
            }

            // Prefer things with a keyword tag, if the filter texts are the same.
            if (!TagsEqual(item1, item2) && item1.FilterText == item2.FilterText)
            {
                return IsKeywordItem(item1);
            }

            // They matched on everything, including preselection values.  Item1 is better if it
            // has a lower MRU index.

            if (!recentItems.IsDefault)
            {
                var item1MRUIndex = GetRecentItemIndex(recentItems, item1);
                var item2MRUIndex = GetRecentItemIndex(recentItems, item2);

                // The one with the lower index is the better one.
                return item1MRUIndex < item2MRUIndex;
            }

            return false;
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