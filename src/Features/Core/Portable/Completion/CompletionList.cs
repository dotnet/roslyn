// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// The set of completions to present to the user.
/// </summary>
public sealed class CompletionList
{
    private readonly Lazy<ImmutableArray<CompletionItem>> _lazyItems;

    /// <summary>
    /// The completion items to present to the user.
    /// </summary>
    [Obsolete($"This property is obsolete. Use {nameof(ItemsList)} instead", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ImmutableArray<CompletionItem> Items => _lazyItems.Value;

    /// <summary>
    /// The completion items to present to the user.
    /// This property is preferred over `Items` because of the flexibility it provides. 
    /// For example, the list can be backed by types like SegmentedList to avoid LOH allocations.
    /// </summary>
    public IReadOnlyList<CompletionItem> ItemsList { get; }

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
    /// The latter is not always the case because each provider is free to make more complex changes 
    /// to the document. If this is the case, <see cref="CompletionItem.IsComplexTextEdit"/> must be
    /// set to <see langword="true"/>.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// The rules used to control behavior of the completion list shown to the user during typing.
    /// </summary>
    public CompletionRules Rules { get; }

    /// <summary>
    /// An optional <see cref="CompletionItem"/> that appears selected in the list presented to the user during suggestion mode.
    /// Suggestion mode disables auto-selection of items in the list, giving preference to the text typed by the user unless a specific item is selected manually.
    /// Specifying a <see cref="SuggestionModeItem"/> is a request that the completion host operate in suggestion mode.
    /// The item specified determines the text displayed and the description associated with it unless a different item is manually selected.
    /// No text is ever inserted when this item is completed, leaving the text the user typed instead.
    /// </summary>
    public CompletionItem? SuggestionModeItem { get; }

    /// <summary>
    /// Whether the items in this list should be the only items presented to the user.
    /// </summary>
    internal bool IsExclusive { get; }

    private CompletionList(
        TextSpan defaultSpan,
        IReadOnlyList<CompletionItem> itemsList,
        CompletionRules? rules,
        CompletionItem? suggestionModeItem,
        bool isExclusive)
    {
        Span = defaultSpan;
        ItemsList = itemsList;
        _lazyItems = new(() => ItemsList.ToImmutableArrayOrEmpty(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        Rules = rules ?? CompletionRules.Default;
        SuggestionModeItem = suggestionModeItem;
        IsExclusive = isExclusive;

        foreach (var item in ItemsList)
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
        CompletionRules? rules = null,
        CompletionItem? suggestionModeItem = null)
    {
        return Create(defaultSpan, items, rules, suggestionModeItem, isExclusive: false);
    }

    internal static CompletionList Create(
        TextSpan defaultSpan,
        IReadOnlyList<CompletionItem> itemsList,
        CompletionRules? rules,
        CompletionItem? suggestionModeItem,
        bool isExclusive)
    {
        return new CompletionList(defaultSpan, itemsList, rules, suggestionModeItem, isExclusive);
    }

    private CompletionList With(
        Optional<TextSpan> span = default,
        Optional<IReadOnlyList<CompletionItem>> itemsList = default,
        Optional<CompletionRules> rules = default,
        Optional<CompletionItem> suggestionModeItem = default)
    {
        var newSpan = span.HasValue ? span.Value : Span;
        var newItemsList = itemsList.HasValue ? itemsList.Value : ItemsList;
        var newRules = rules.HasValue ? rules.Value : Rules;
        var newSuggestionModeItem = suggestionModeItem.HasValue ? suggestionModeItem.Value : SuggestionModeItem;

        if (newSpan == Span &&
            newItemsList == ItemsList &&
            newRules == Rules &&
            newSuggestionModeItem == SuggestionModeItem)
        {
            return this;
        }
        else
        {
            return Create(newSpan, newItemsList, newRules, newSuggestionModeItem, IsExclusive);
        }
    }

    /// <summary>
    /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="DefaultSpan"/> property changed.
    /// </summary>
    [Obsolete("Not used anymore.  Use WithSpan instead.", error: true)]
    public CompletionList WithDefaultSpan(TextSpan span)
        => With(span: span);

    public CompletionList WithSpan(TextSpan span)
        => With(span: span);

#pragma warning disable RS0030 // Do not used banned APIs
    /// <summary>
    /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="Items"/> property changed.
    /// </summary>
    public CompletionList WithItems(ImmutableArray<CompletionItem> items)
#pragma warning restore RS0030 // Do not used banned APIs
        => With(itemsList: items);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="ItemsList"/> property changed.
    /// </summary>
    internal CompletionList WithItemsList(IReadOnlyList<CompletionItem> itemsList)
        => With(itemsList: new(itemsList));

    /// <summary>
    /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="Rules"/> property changed.
    /// </summary>
    public CompletionList WithRules(CompletionRules rules)
        => With(rules: rules);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionList"/> with the <see cref="SuggestionModeItem"/> property changed.
    /// </summary>
    public CompletionList WithSuggestionModeItem(CompletionItem suggestionModeItem)
        => With(suggestionModeItem: suggestionModeItem);

    /// <summary>
    /// The default <see cref="CompletionList"/> returned when no items are found to populate the list.
    /// </summary>
    public static readonly CompletionList Empty = new(
        default, [], CompletionRules.Default,
        suggestionModeItem: null, isExclusive: false);

    internal bool IsEmpty => ItemsList.Count == 0 && SuggestionModeItem is null;
}
