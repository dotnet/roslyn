// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// One of many possible completions used to form the completion list presented to the user.
    /// </summary>
    [DebuggerDisplay("{DisplayText}")]
    public sealed class CompletionItem : IComparable<CompletionItem>
    {
        private readonly string? _filterText;
        private string? _lazyEntireDisplayText;
        private ImmutableDictionary<string, string>? _lazyPropertiesAsImmutableDictionary;
        private readonly ImmutableArray<KeyValuePair<string, string>> _properties;

        /// <summary>
        /// The text that is displayed to the user.
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// An optional prefix to be displayed prepended to <see cref="DisplayText"/>. Can be null.
        /// Pattern-matching of user input will not be performed against this, but only against <see
        /// cref="DisplayText"/>.
        /// </summary>
        public string DisplayTextPrefix { get; }

        /// <summary>
        /// An optional suffix to be displayed appended to <see cref="DisplayText"/>. Can be null.
        /// Pattern-matching of user input will not be performed against this, but only against <see
        /// cref="DisplayText"/>.
        /// </summary>
        public string DisplayTextSuffix { get; }

        /// <summary>
        /// The text used to determine if the item matches the filter and is show in the list.
        /// This is often the same as <see cref="DisplayText"/> but may be different in certain circumstances.
        /// </summary>
        public string FilterText => _filterText ?? DisplayText;

        /// <summary>
        /// If provided, each additional string would be used in the same way as <see cref="FilterText"/> for item matching.
        /// However, there's a key difference: matches of <see cref="AdditionalFilterTexts"/> is considered inferior than matches
        /// of <see cref="FilterText"/> when they have identical pattern matching result.
        /// </summary>
        internal ImmutableArray<string> AdditionalFilterTexts { get; init; } = ImmutableArray<string>.Empty;

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="DisplayText"/> is identical to  <see cref="FilterText"/>. 
        /// Otherwise returns <see langword="false"/>.
        /// Be aware that this value is independent from <see cref="HasAdditionalFilterTexts"/> and could return <see langword="false"/> 
        /// even if <see cref="HasAdditionalFilterTexts"/> is <see langword="true"/>.
        /// </summary>
        internal bool HasDifferentFilterText => _filterText != null;

        internal bool HasAdditionalFilterTexts => !AdditionalFilterTexts.IsEmpty;

        /// <summary>
        /// The text used to determine the order that the item appears in the list.
        /// This is often the same as the <see cref="DisplayText"/> but may be different in certain circumstances.
        /// </summary>
        public string SortText { get; }

        /// <summary>
        /// Descriptive text to place after <see cref="DisplayText"/> in the display layer.  Should
        /// be short as it will show up in the UI.  Display will present this in a way to distinguish
        /// this from the normal text (for example, by fading out and right-aligning).
        /// </summary>
        public string InlineDescription { get; }

        /// <summary>
        /// The span of the syntax element associated with this item.
        /// 
        /// The span identifies the text in the document that is used to filter the initial list presented to the user,
        /// and typically represents the region of the document that will be changed if this item is committed.
        /// The latter is not always true because individual provider is free to make more complex changes to the document.
        /// If this is the case, the provider should set <see cref="IsComplexTextEdit"/> to true.
        /// </summary>
        public TextSpan Span { get; internal set; }

        /// <summary>
        /// Additional information attached to a completion item by it creator.
        /// </summary>
        public ImmutableDictionary<string, string> Properties
        {
            get
            {
                if (_lazyPropertiesAsImmutableDictionary is null)
                    _lazyPropertiesAsImmutableDictionary = _properties.ToImmutableDictionary();

                return _lazyPropertiesAsImmutableDictionary;
            }
        }

        internal ImmutableArray<KeyValuePair<string, string>> GetProperties()
        {
            return _properties;
        }

        internal bool TryGetProperty(string name, [NotNullWhen(true)] out string? value)
        {
            if (_lazyPropertiesAsImmutableDictionary is not null)
                return _lazyPropertiesAsImmutableDictionary.TryGetValue(name, out value);

            foreach ((var propName, var propValue) in _properties)
            {
                if (name == propName)
                {
                    value = propValue;
                    return true;
                }
            }

            value = null;
            return false;
        }

        internal string GetProperty(string name)
        {
            if (TryGetProperty(name, out var value))
                return value;

            // Let ImmutableDictionary handle throwing
            if (_lazyPropertiesAsImmutableDictionary is not null)
                return _lazyPropertiesAsImmutableDictionary[name];

            throw new KeyNotFoundException($"Property {name} not found");
        }

        /// <summary>
        /// Descriptive tags from <see cref="Tags.WellKnownTags"/>.
        /// These tags may influence how the item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// Rules that declare how this item should behave.
        /// </summary>
        public CompletionItemRules Rules { get; }

        /// <summary>
        /// Returns true if this item's text edit requires complex resolution.
        /// An edit is considered complex if the span of the change is different from
        /// specified by <see cref="Span"/>.
        /// 
        /// Example of an item type requiring complex resolution is C#/VB override completion.
        /// </summary>
        public bool IsComplexTextEdit { get; }

        /// <summary>
        /// The name of the <see cref="CompletionProvider"/> that created this 
        /// <see cref="CompletionItem"/>. Not available to clients. Only used by 
        /// the Completion subsystem itself for things like getting description text
        /// and making additional change during commit.
        /// </summary>
        internal string? ProviderName { get; set; }

        /// <summary>
        /// The automation text to use when narrating the completion item. If set to
        /// null, narration will use the <see cref="DisplayText"/> instead.
        /// </summary>
        internal string? AutomationText { get; set; }

        internal CompletionItemFlags Flags { get; set; }

        private CompletionItem(
            string? displayText,
            string? filterText,
            string? sortText,
            TextSpan span,
            ImmutableArray<KeyValuePair<string, string>> properties,
            ImmutableArray<string> tags,
            CompletionItemRules? rules,
            string? displayTextPrefix,
            string? displayTextSuffix,
            string? inlineDescription,
            bool isComplexTextEdit)
        {
            DisplayText = displayText ?? "";
            DisplayTextPrefix = displayTextPrefix ?? "";
            DisplayTextSuffix = displayTextSuffix ?? "";
            SortText = sortText ?? DisplayText;
            InlineDescription = inlineDescription ?? "";
            Span = span;
            Tags = tags.NullToEmpty();
            Rules = rules ?? CompletionItemRules.Default;
            IsComplexTextEdit = isComplexTextEdit;

            _properties = properties.NullToEmpty();
            if (_properties.Length > 10)
            {
                // Prefer to just keep an ImmutableArray, but performance on large property collections
                //  (quite uncommon) dictate falling back to a non-linear lookup data structure.
                _lazyPropertiesAsImmutableDictionary = _properties.ToImmutableDictionary();
            }

            if (!DisplayText.Equals(filterText ?? "", StringComparison.Ordinal))
            {
                _filterText = filterText;
            }
        }

        // binary back compat overload
        public static CompletionItem Create(
            string? displayText,
            string? filterText,
            string? sortText,
            ImmutableDictionary<string, string>? properties,
            ImmutableArray<string> tags,
            CompletionItemRules? rules)
        {
            return Create(displayText, filterText, sortText, properties, tags, rules, displayTextPrefix: null, displayTextSuffix: null);
        }

        // binary back compat overload
        public static CompletionItem Create(
            string? displayText,
            string? filterText,
            string? sortText,
            ImmutableDictionary<string, string>? properties,
            ImmutableArray<string> tags,
            CompletionItemRules? rules,
            string? displayTextPrefix,
            string? displayTextSuffix)
        {
            return Create(displayText, filterText, sortText, properties, tags, rules, displayTextPrefix, displayTextSuffix, inlineDescription: null);
        }

        // binary back compat overload
        public static CompletionItem Create(
            string? displayText,
            string? filterText,
            string? sortText,
            ImmutableDictionary<string, string>? properties,
            ImmutableArray<string> tags,
            CompletionItemRules? rules,
            string? displayTextPrefix,
            string? displayTextSuffix,
            string? inlineDescription)
        {
            return Create(
                displayText, filterText, sortText, properties, tags, rules, displayTextPrefix,
                displayTextSuffix, inlineDescription, isComplexTextEdit: false);
        }

        public static CompletionItem Create(
            string? displayText,
            string? filterText = null,
            string? sortText = null,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableArray<string> tags = default,
            CompletionItemRules? rules = null,
            string? displayTextPrefix = null,
            string? displayTextSuffix = null,
            string? inlineDescription = null,
            bool isComplexTextEdit = false)
        {
            var result = CreateInternal(
                displayText, filterText, sortText, properties.AsImmutableOrNull(), tags, rules, displayTextPrefix,
                displayTextSuffix, inlineDescription, isComplexTextEdit);

            result._lazyPropertiesAsImmutableDictionary = properties;

            return result;
        }

        internal static CompletionItem CreateInternal(
            string? displayText,
            string? filterText = null,
            string? sortText = null,
            ImmutableArray<KeyValuePair<string, string>> properties = default,
            ImmutableArray<string> tags = default,
            CompletionItemRules? rules = null,
            string? displayTextPrefix = null,
            string? displayTextSuffix = null,
            string? inlineDescription = null,
            bool isComplexTextEdit = false)
        {
            return new CompletionItem(
                span: default,
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                properties: properties,
                tags: tags,
                rules: rules,
                displayTextPrefix: displayTextPrefix,
                displayTextSuffix: displayTextSuffix,
                inlineDescription: inlineDescription,
                isComplexTextEdit: isComplexTextEdit);
        }

        /// <summary>
        /// Creates a new <see cref="CompletionItem"/>
        /// </summary>
        /// <param name="displayText">The text that is displayed to the user.</param>
        /// <param name="filterText">The text used to determine if the item matches the filter and is show in the list.</param>
        /// <param name="sortText">The text used to determine the order that the item appears in the list.</param>
        /// <param name="span">The span of the syntax element in the document associated with this item.</param>
        /// <param name="properties">Additional information.</param>
        /// <param name="tags">Descriptive tags that may influence how the item is displayed.</param>
        /// <param name="rules">The rules that declare how this item should behave.</param>
        /// <returns></returns>
        [Obsolete("Use the Create overload that does not take a span", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CompletionItem Create(
            string displayText,
            string filterText,
            string sortText,
            TextSpan span,
            ImmutableDictionary<string, string>? properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules)
        {
            var result = new CompletionItem(
                span: span,
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                properties: properties.AsImmutableOrNull(),
                tags: tags,
                rules: rules,
                displayTextPrefix: null,
                displayTextSuffix: null,
                inlineDescription: null,
                isComplexTextEdit: false);

            result._lazyPropertiesAsImmutableDictionary = properties;

            return result;
        }

        private CompletionItem With(
            Optional<TextSpan> span = default,
            Optional<string> displayText = default,
            Optional<string> filterText = default,
            Optional<string> sortText = default,
            Optional<ImmutableArray<KeyValuePair<string, string>>> properties = default,
            Optional<ImmutableArray<string>> tags = default,
            Optional<CompletionItemRules> rules = default,
            Optional<string> displayTextPrefix = default,
            Optional<string> displayTextSuffix = default,
            Optional<string> inlineDescription = default,
            Optional<bool> isComplexTextEdit = default,
            Optional<ImmutableArray<string>> additionalFilterTexts = default)
        {
            var newSpan = span.HasValue ? span.Value : Span;
            var newDisplayText = displayText.HasValue ? displayText.Value : DisplayText;
            var newFilterText = filterText.HasValue ? filterText.Value : FilterText;
            var newSortText = sortText.HasValue ? sortText.Value : SortText;
            var newInlineDescription = inlineDescription.HasValue ? inlineDescription.Value : InlineDescription;
            var newProperties = properties.HasValue ? properties.Value : _properties;
            var newTags = tags.HasValue ? tags.Value : Tags;
            var newRules = rules.HasValue ? rules.Value : Rules;
            var newDisplayTextPrefix = displayTextPrefix.HasValue ? displayTextPrefix.Value : DisplayTextPrefix;
            var newDisplayTextSuffix = displayTextSuffix.HasValue ? displayTextSuffix.Value : DisplayTextSuffix;
            var newIsComplexTextEdit = isComplexTextEdit.HasValue ? isComplexTextEdit.Value : IsComplexTextEdit;
            var newAdditionalFilterTexts = additionalFilterTexts.HasValue ? additionalFilterTexts.Value.NullToEmpty() : AdditionalFilterTexts;

            if (newSpan == Span &&
                newDisplayText == DisplayText &&
                newFilterText == FilterText &&
                newSortText == SortText &&
                newProperties == _properties &&
                newTags == Tags &&
                newRules == Rules &&
                newDisplayTextPrefix == DisplayTextPrefix &&
                newDisplayTextSuffix == DisplayTextSuffix &&
                newInlineDescription == InlineDescription &&
                newIsComplexTextEdit == IsComplexTextEdit &&
                newAdditionalFilterTexts == AdditionalFilterTexts)
            {
                return this;
            }

            return new CompletionItem(
                displayText: newDisplayText,
                filterText: newFilterText,
                span: newSpan,
                sortText: newSortText,
                properties: newProperties,
                tags: newTags,
                rules: newRules,
                displayTextPrefix: newDisplayTextPrefix,
                displayTextSuffix: newDisplayTextSuffix,
                inlineDescription: newInlineDescription,
                isComplexTextEdit: newIsComplexTextEdit)
            {
                AutomationText = AutomationText,
                ProviderName = ProviderName,
                Flags = Flags,
                AdditionalFilterTexts = newAdditionalFilterTexts
            };
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Span"/> property changed.
        /// </summary>
        [Obsolete("Not used anymore.  CompletionList.Span is used to control the span used for filtering.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CompletionItem WithSpan(TextSpan span)
            => this;

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="DisplayText"/> property changed.
        /// </summary>
        public CompletionItem WithDisplayText(string text)
            => With(displayText: text);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="DisplayTextPrefix"/> property changed.
        /// </summary>
        public CompletionItem WithDisplayTextPrefix(string displayTextPrefix)
            => With(displayTextPrefix: displayTextPrefix);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="DisplayTextSuffix"/> property changed.
        /// </summary>
        public CompletionItem WithDisplayTextSuffix(string displayTextSuffix)
            => With(displayTextSuffix: displayTextSuffix);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="FilterText"/> property changed.
        /// </summary>
        public CompletionItem WithFilterText(string text)
            => With(filterText: text);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="SortText"/> property changed.
        /// </summary>
        public CompletionItem WithSortText(string text)
            => With(sortText: text);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the specified property changed.
        /// </summary>
        public CompletionItem WithProperties(ImmutableDictionary<string, string> properties)
        {
            var result = With(properties: properties.AsImmutableOrNull());

            result._lazyPropertiesAsImmutableDictionary = properties;

            return result;
        }

        internal CompletionItem WithProperties(ImmutableArray<KeyValuePair<string, string>> properties)
            => With(properties: properties);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the specified property.
        /// </summary>
        public CompletionItem AddProperty(string name, string value)
            => With(properties: GetProperties().Add(new KeyValuePair<string, string>(name, value)));

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Tags"/> property changed.
        /// </summary>
        public CompletionItem WithTags(ImmutableArray<string> tags)
            => With(tags: tags);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with a tag added to the <see cref="Tags"/> collection.
        /// </summary>
        public CompletionItem AddTag(string tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (Tags.Contains(tag))
            {
                return this;
            }
            else
            {
                return With(tags: Tags.Add(tag));
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Rules"/> property changed.
        /// </summary>
        public CompletionItem WithRules(CompletionItemRules rules)
            => With(rules: rules);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="IsComplexTextEdit"/> property changed.
        /// </summary>
        public CompletionItem WithIsComplexTextEdit(bool isComplexTextEdit)
            => With(isComplexTextEdit: isComplexTextEdit);

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="AdditionalFilterTexts"/> property changed.
        /// </summary>
        internal CompletionItem WithAdditionalFilterTexts(ImmutableArray<string> additionalFilterTexts)
            => With(additionalFilterTexts: additionalFilterTexts);

        int IComparable<CompletionItem>.CompareTo([AllowNull] CompletionItem other)
        {
            if (other == null)
            {
                return 1;
            }

            // Make sure expanded items are listed after non-expanded ones.
            // Note that in our ItemManager at async-completion layer, we implicitly
            // rely on this behavior when combining delayed expanded items list with 
            // non-expanded items (to avoid sorting again). if this changed, we need
            // to make sure to sort items properly in ItemManager.
            var thisIsExpandItem = Flags.IsExpanded();
            var otherIsExpandItem = other.Flags.IsExpanded();

            if (thisIsExpandItem == otherIsExpandItem)
            {
                var result = StringComparer.OrdinalIgnoreCase.Compare(SortText, other.SortText);
                if (result == 0)
                {
                    result = StringComparer.OrdinalIgnoreCase.Compare(GetEntireDisplayText(), other.GetEntireDisplayText());
                }

                return result;
            }
            else if (thisIsExpandItem)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        internal string GetEntireDisplayText()
            => _lazyEntireDisplayText ??= DisplayTextPrefix + DisplayText + DisplayTextSuffix;

        public override string ToString() => GetEntireDisplayText();
    }
}
