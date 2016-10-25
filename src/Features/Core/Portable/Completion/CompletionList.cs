﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The set of completions to present to the user.
    /// </summary>
    public sealed class CompletionList
    {
        /// <summary>
        /// The completion items to present to the user.
        /// </summary>
        public ImmutableArray<CompletionItem> Items { get; }

        /// <summary>
        /// The span of the syntax element at the caret position when the <see cref="CompletionList"/> was created.
        /// Individual <see cref="CompletionItem"/> spans may vary.
        /// </summary>
        [Obsolete("Not used anymore.  CompletionList.Span is used instead.", error: true)]
        public TextSpan DefaultSpan { get; }

        /// <summary>
        /// The span of the syntax element at the caret position when the <see cref="CompletionList"/> 
        /// was created.
        /// 
        /// The span identifies the text in the document that is used to filter the initial list 
        /// presented to the user, and typically represents the region of the document that will 
        /// be changed if this item is committed.
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        /// The rules used to control behavior of the completion list shown to the user during typing.
        /// </summary>
        public CompletionRules Rules { get; }

        /// <summary>
        /// An optional <see cref="CompletionItem"/> that appears selected in the list presented to the user during suggestion mode.
        /// Suggestion mode disables autoselection of items in the list, giving preference to the text typed by the user unless a specific item is selected manually.
        /// Specifying a <see cref="SuggestionModeItem"/> is a request that the completion host operate in suggestion mode.
        /// The item specified determines the text displayed and the description associated with it unless a different item is manually selected.
        /// No text is ever inserted when this item is completed, leaving the text the user typed instead.
        /// </summary>
        public CompletionItem SuggestionModeItem { get; }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        internal bool IsExclusive { get; }

        private CompletionList(
            TextSpan defaultSpan,
            ImmutableArray<CompletionItem> items,
            CompletionRules rules,
            CompletionItem suggestionModeItem,
            bool isExclusive)
        {
            Span = defaultSpan;

            Items = items.NullToEmpty();
            Rules = rules ?? CompletionRules.Default;
            SuggestionModeItem = suggestionModeItem;
            IsExclusive = isExclusive;

            foreach (var item in Items)
            {
                item.Span = defaultSpan;
            }
        }

        /// <summary>
        /// Creates a new <see cref="CompletionList"/> instance.
        /// </summary>
        /// <param name="defaultSpan">The span of the syntax element at the caret position when the <see cref="CompletionList"/> was created.</param>
        /// <param name="items">The completion items to present to the user.</param>
        /// <param name="rules">The rules used to control behavior of the completion list shown to the user during typing.</param>
        /// <param name="suggestionModeItem">An optional <see cref="CompletionItem"/> that appears selected in the list presented to the user during suggestion mode.</param>
        /// <returns></returns>
        public static CompletionList Create(
            TextSpan defaultSpan,
            ImmutableArray<CompletionItem> items,
            CompletionRules rules = null,
            CompletionItem suggestionModeItem = null)
        {
            return Create(defaultSpan, items, rules, suggestionModeItem, isExclusive: false);
        }

        internal static CompletionList Create(
            TextSpan defaultSpan,
            ImmutableArray<CompletionItem> items,
            CompletionRules rules,
            CompletionItem suggestionModeItem,
            bool isExclusive)
        {
            return new CompletionList(defaultSpan, items, rules, suggestionModeItem, isExclusive);
        }

        private CompletionList With(
            Optional<TextSpan> span = default(Optional<TextSpan>),
            Optional<ImmutableArray<CompletionItem>> items = default(Optional<ImmutableArray<CompletionItem>>),
            Optional<CompletionRules> rules = default(Optional<CompletionRules>),
            Optional<CompletionItem> suggestionModeItem = default(Optional<CompletionItem>))
        {
            var newSpan = span.HasValue ? span.Value : this.Span;
            var newItems = items.HasValue ? items.Value : this.Items;
            var newRules = rules.HasValue ? rules.Value : this.Rules;
            var newSuggestionModeItem = suggestionModeItem.HasValue ? suggestionModeItem.Value : this.SuggestionModeItem;

            if (newSpan == this.Span &&
                newItems == this.Items &&
                newRules == this.Rules &&
                newSuggestionModeItem == this.SuggestionModeItem)
            {
                return this;
            }
            else
            {
                return Create(newSpan, newItems, newRules, newSuggestionModeItem);
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="DefaultSpan"/> property changed.
        /// </summary>
        [Obsolete("Not used anymore.  Use WithSpan instead.", error: true)]
        public CompletionList WithDefaultSpan(TextSpan span)
        {
            return With(span: span);
        }

        public CompletionList WithSpan(TextSpan span)
        {
            return With(span: span);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="Items"/> property changed.
        /// </summary>
        public CompletionList WithItems(ImmutableArray<CompletionItem> items)
        {
            return With(items: items);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="Rules"/> property changed.
        /// </summary>
        public CompletionList WithRules(CompletionRules rules)
        {
            return With(rules: rules);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="SuggestionModeItem"/> property changed.
        /// </summary>
        public CompletionList WithSuggestionModeItem(CompletionItem suggestionModeItem)
        {
            return With(suggestionModeItem: suggestionModeItem);
        }

        /// <summary>
        /// The default <see cref="CompletionList"/> returned when no items are found to populate the list.
        /// </summary>
        public static readonly CompletionList Empty = new CompletionList(
            default(TextSpan), default(ImmutableArray<CompletionItem>), CompletionRules.Default,
            suggestionModeItem: null, isExclusive: false);
    }
}