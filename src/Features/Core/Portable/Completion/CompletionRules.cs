// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal class CompletionRules
    {
        private readonly static char[] s_defaultCommitCharacters = new[]
            {
                ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
                ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
                '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
            };

        private readonly object _gate = new object();
        private readonly AbstractCompletionService _completionService;
        private readonly Dictionary<string, PatternMatcher> _patternMatcherMap = new Dictionary<string, PatternMatcher>();
        private readonly Dictionary<string, PatternMatcher> _fallbackPatternMatcherMap = new Dictionary<string, PatternMatcher>();
        private static readonly CultureInfo EnUSCultureInfo = new CultureInfo("en-US");

        public CompletionRules(AbstractCompletionService completionService)
        {
            _completionService = completionService;
        }

        protected PatternMatcher GetPatternMatcher(string value, CultureInfo culture)
        {
            lock (_gate)
            {
                PatternMatcher patternMatcher;
                if (!_patternMatcherMap.TryGetValue(value, out patternMatcher))
                {
                    patternMatcher = new PatternMatcher(value, culture, verbatimIdentifierPrefixIsWordCharacter: true);
                    _patternMatcherMap.Add(value, patternMatcher);
                }

                return patternMatcher;
            }
        }

        protected PatternMatcher GetFallbackPatternMatcher(string value)
        {
            lock (_gate)
            {
                PatternMatcher patternMatcher;
                if (!_fallbackPatternMatcherMap.TryGetValue(value, out patternMatcher))
                {
                    patternMatcher = new PatternMatcher(value, EnUSCultureInfo, verbatimIdentifierPrefixIsWordCharacter: true);
                    _fallbackPatternMatcherMap.Add(value, patternMatcher);
                }

                return patternMatcher;
            }
        }

        /// <summary>
        /// Returns true if the completion item matches the filter text typed so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        public virtual bool MatchesFilterText(CompletionItem item, string filterText, CompletionTriggerInfo triggerInfo, CompletionFilterReason filterReason)
        {
            // If the user hasn't typed anything, and this item was preselected, or was in the
            // MRU list, then we definitely want to include it.
            if (filterText.Length == 0)
            {
                if (item.Preselect || _completionService.GetMRUIndex(item) < 0)
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

        protected PatternMatch? GetMatch(CompletionItem item, string filterText)
        {
            var patternMatcher = this.GetPatternMatcher(_completionService.GetCultureSpecificQuirks(filterText), CultureInfo.CurrentCulture);
            var match = patternMatcher.GetFirstMatch(_completionService.GetCultureSpecificQuirks(item.FilterText));

            if (match != null)
            {
                return match;
            }

            // Start with the culture-specific comparison, and fall back to en-US.
            if (!CultureInfo.CurrentCulture.Equals(EnUSCultureInfo))
            {
                patternMatcher = this.GetFallbackPatternMatcher(_completionService.GetCultureSpecificQuirks(filterText));
                match = patternMatcher.GetFirstMatch(_completionService.GetCultureSpecificQuirks(item.FilterText));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
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

        /// <summary>
        /// Returns true if item1 is a better completion item than item2 given the provided filter
        /// text, or false if it is not better.
        /// </summary>
        public virtual bool IsBetterFilterMatch(CompletionItem item1, CompletionItem item2, string filterText, CompletionTriggerInfo triggerInfo, CompletionFilterReason filterReason)
        {
            var match1 = GetMatch(item1, _completionService.GetCultureSpecificQuirks(filterText));
            var match2 = GetMatch(item2, _completionService.GetCultureSpecificQuirks(filterText));

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
            // item1 is better if it is preselected, otherwise it it worse.
            if (item1.Preselect != item2.Preselect)
            {
                return item1.Preselect;
            }

            // Prefer things with a keyword glyph, if the filter texts are the same.
            if (item1.Glyph != item2.Glyph && item1.FilterText == item2.FilterText)
            {
                return item1.Glyph == Glyph.Keyword;
            }

            // They matched on everything, including preselection values.  Item1 is better if it
            // has a lower MRU index.

            var item1MRUIndex = _completionService.GetMRUIndex(item1);
            var item2MRUIndex = _completionService.GetMRUIndex(item2);

            // The one with the lower index is the better one.
            return item1MRUIndex < item2MRUIndex;
        }

        protected virtual int CompareMatches(PatternMatch match1, PatternMatch match2, CompletionItem item1, CompletionItem item2)
        {
            return match1.CompareTo(match2);
        }

        /// <summary>
        /// Returns true if the completion item should be "soft" selected, or false if it should be "hard"
        /// selected.
        /// </summary>
        public virtual bool ShouldSoftSelectItem(CompletionItem item, string filterText, CompletionTriggerInfo triggerInfo)
        {
            return filterText.Length == 0 && !item.Preselect;
        }

        /// <summary>
        /// Called by completion engine when a completion item is committed.  Completion rules can
        /// use this information to affect future calls to MatchesFilterText or IsBetterFilterMatch.
        /// </summary>
        public virtual void CompletionItemCommitted(CompletionItem item)
        {
            _completionService.CompletionItemCommitted(item);
        }

        /// <summary>
        /// Returns true if item1 and item2 are similar enough that only one should be shown in the completion list; otherwise, false.
        /// </summary>
        public virtual bool ItemsMatch(CompletionItem item1, CompletionItem item2)
        {
            return item1.FilterSpan == item2.FilterSpan
                && item1.SortText == item2.SortText;
        }

        protected virtual TextChange GetTextChangeCore(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        }

        /// <summary>
        /// The text change that will be made when this item is committed.  The text change includes
        /// both the span of text to replace (respective to the original document text when this
        /// completion item was created) and the text to replace it with.  The span will be adjusted
        /// automatically by the completion engine to fit on the current text using "EdgeInclusive"
        /// semantics.
        /// </summary>
        public TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            var result = selectedItem.Rules.GetTextChange(selectedItem, ch, textTypedSoFar);

            return result.HasValue
                ? result.Value
                : GetTextChangeCore(selectedItem, ch, textTypedSoFar);
        }

        protected virtual bool IsCommitCharacterCore(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return s_defaultCommitCharacters.Contains(ch);
        }

        /// <summary>
        /// Returns true if the character is one that can commit the specified completion item. A
        /// character will be checked to see if it should filter an item.  If not, it will be checked
        /// to see if it should commit that item.  If it does neither, then completion will be
        /// dismissed.
        /// </summary>
        public bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            var result = completionItem.Rules.IsCommitCharacter(completionItem, ch, textTypedSoFar);

            return result.HasValue
                ? result.Value
                : IsCommitCharacterCore(completionItem, ch, textTypedSoFar);
        }

        protected virtual bool IsFilterCharacterCore(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return false;
        }

        /// <summary>
        /// Returns true if the character typed should be used to filter the specified completion
        /// item.  A character will be checked to see if it should filter an item.  If not, it will be
        /// checked to see if it should commit that item.  If it does neither, then completion will
        /// be dismissed.
        /// </summary>
        public bool IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            var result = completionItem.Rules.IsFilterCharacter(completionItem, ch, textTypedSoFar);

            return result.HasValue
                ? result.Value
                : IsFilterCharacterCore(completionItem, ch, textTypedSoFar);
        }

        protected virtual bool SendEnterThroughToEditorCore(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }

        /// <summary>
        /// Returns true if the enter key that was typed should also be sent through to the editor
        /// after committing the provided completion item.
        /// </summary>
        public bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            var result = completionItem.Rules.SendEnterThroughToEditor(completionItem, textTypedSoFar, options);

            return result.HasValue
                ? result.Value
                : SendEnterThroughToEditorCore(completionItem, textTypedSoFar, options);
        }
    }
}
