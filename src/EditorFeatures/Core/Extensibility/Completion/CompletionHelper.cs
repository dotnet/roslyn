// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionHelper
    {
        private readonly CompletionRules _rules;
        private readonly string _language;
        
        public CompletionService CompletionService { get; }

        protected CompletionHelper(CompletionService completionService)
        {
            CompletionService = completionService;
            _language = CompletionService.Language;
            _rules = CompletionService.GetRules();
        }

        public static CompletionHelper GetHelper(
            Workspace workspace, string language, CompletionService completionService)
        {
            var ls = workspace.Services.GetLanguageServices(language);
            if (ls != null)
            {
                var factory = ls.GetService<CompletionHelperFactory>();
                if (factory != null)
                {
                    return factory.CreateCompletionHelper(completionService);
                }

                if (completionService != null)
                {
                    return new CompletionHelper(completionService);
                }
            }

            return null;
        }

        public static CompletionHelper GetHelper(Document document, CompletionService service)
        {
            return GetHelper(document.Project.Solution.Workspace, document.Project.Language, service);
        }

        public IReadOnlyList<TextSpan> GetHighlightedSpans(CompletionItem completionItem, string filterText)
        {
            var match = GetMatch(completionItem, filterText, includeMatchSpans: true);
            return match?.MatchedSpans;
        }

        /// <summary>
        /// If true then a [TAB] after a question mark brings up completion.
        /// </summary>
        public virtual bool QuestionTabInvokesSnippetCompletion => false;

        /// <summary>
        /// Returns true if the completion item matches the filter text typed so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public virtual bool MatchesFilterText(CompletionItem item, string filterText, CompletionTrigger trigger, CompletionFilterReason filterReason, ImmutableArray<string> recentItems = default(ImmutableArray<string>))
        {
            // If the user hasn't typed anything, and this item was preselected, or was in the
            // MRU list, then we definitely want to include it.
            if (filterText.Length == 0)
            {
                if (item.Rules.Preselect || (!recentItems.IsDefault && GetRecentItemIndex(recentItems, item) < 0))
                {
                    return true;
                }
            }

            if (IsAllDigits(filterText))
            {
                // The user is just typing a number.  We never want this to match against
                // anything we would put in a completion list.
                return false;
            }

            return GetMatch(item, filterText) != null;
        }

        protected static int GetRecentItemIndex(ImmutableArray<string> recentItems, CompletionItem item)
        {
            var index = recentItems.IndexOf(item.DisplayText);
            return -index;
        }

        protected static bool IsAllDigits(string filterText)
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

        protected PatternMatch? GetMatch(CompletionItem item, string filterText)
        {
            return GetMatch(item, filterText, includeMatchSpans: false);
        }

        protected PatternMatch? GetMatch(CompletionItem item, string filterText, bool includeMatchSpans)
        {
            var patternMatcher = this.GetPatternMatcher(filterText, CultureInfo.CurrentCulture);
            var match = patternMatcher.GetFirstMatch(item.FilterText, includeMatchSpans);

            if (match != null)
            {
                return match;
            }

            // Start with the culture-specific comparison, and fall back to en-US.
            if (!CultureInfo.CurrentCulture.Equals(EnUSCultureInfo))
            {
                patternMatcher = this.GetEnUSPatternMatcher(filterText);
                match = patternMatcher.GetFirstMatch(item.FilterText);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private readonly object _gate = new object();
        private readonly Dictionary<string, PatternMatcher> _patternMatcherMap = new Dictionary<string, PatternMatcher>();
        private readonly Dictionary<string, PatternMatcher> _fallbackPatternMatcherMap = new Dictionary<string, PatternMatcher>();
        internal static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");

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

        protected PatternMatcher GetPatternMatcher(string value, CultureInfo culture)
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
        public virtual bool IsBetterFilterMatch(CompletionItem item1, CompletionItem item2, string filterText, CompletionTrigger trigger, CompletionFilterReason filterReason, ImmutableArray<string> recentItems = default(ImmutableArray<string>))
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
            if (item1.Rules.Preselect != item2.Rules.Preselect)
            {
                return item1.Rules.Preselect;
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

        internal static bool TagsEqual(CompletionItem item1, CompletionItem item2)
        {
            return TagsEqual(item1.Tags, item2.Tags);
        }

        internal static bool TagsEqual(ImmutableArray<string> tags1, ImmutableArray<string> tags2)
        {
            return tags1 == tags2 || System.Linq.Enumerable.SequenceEqual(tags1, tags2);
        }

        protected static bool IsKeywordItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Keyword);
        }

        protected static bool IsEnumMemberItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.EnumMember);
        }

        protected int GetPrefixLength(string text, string pattern)
        {
            int x = 0;
            while (x < text.Length && x < pattern.Length && char.ToUpper(text[x]) == char.ToUpper(pattern[x]))
            {
                x++;
            }

            return x;
        }

        protected int CompareMatches(PatternMatch match1, PatternMatch match2, CompletionItem item1, CompletionItem item2)
        {
            int diff;

            diff = PatternMatch.CompareType(match1, match2);
            if (diff != 0)
            {
                return diff;
            }

            diff = PatternMatch.CompareCamelCase(match1, match2);
            if (diff != 0)
            {
                return diff;
            }

            // argument names are not prefered
            if (IsArgumentName(item1) && !IsArgumentName(item2))
            {
                return 1;
            }
            else if (IsArgumentName(item2) && !IsArgumentName(item1))
            {
                return -1;
            }

            // preselected items are prefered
            if (item1.Rules.Preselect && !item2.Rules.Preselect)
            {
                return -1;
            }
            else if (item2.Rules.Preselect && !item1.Rules.Preselect)
            {
                return 1;
            }

            diff = PatternMatch.CompareCase(match1, match2);
            if (diff != 0)
            {
                return diff;
            }

            diff = PatternMatch.ComparePunctuation(match1, match2);
            if (diff != 0)
            {
                return diff;
            }

            return 0;
        }

        protected bool IsArgumentName(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.ArgumentName);
        }

        private static bool TextTypedSoFarMatchesItem(CompletionItem item, char ch, string textTypedSoFar)
        {
            var textTypedWithChar = textTypedSoFar + ch;
            return item.DisplayText.StartsWith(textTypedWithChar, StringComparison.CurrentCultureIgnoreCase) ||
                item.FilterText.StartsWith(textTypedWithChar, StringComparison.CurrentCultureIgnoreCase);
        }

        private static StringComparison GetComparision(bool isCaseSensitive)
        {
            return isCaseSensitive? StringComparison.CurrentCulture: StringComparison.CurrentCultureIgnoreCase;
        }

        /// <summary>
        /// Returns true if the completion item should be "soft" selected, or false if it should be "hard"
        /// selected.
        /// </summary>
        public virtual bool ShouldSoftSelectItem(CompletionItem item, string filterText, CompletionTrigger trigger)
        {
            return filterText.Length == 0 && !item.Rules.Preselect;
        }

        protected bool IsObjectCreationItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.ObjectCreation);
        }
    }
}