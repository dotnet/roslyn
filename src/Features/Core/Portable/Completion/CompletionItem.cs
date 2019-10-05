// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// One of many possible completions used to form the completion list presented to the user.
    /// </summary>
    [DebuggerDisplay("{DisplayText}")]
    public sealed class CompletionItem : IComparable<CompletionItem>
    {
        private readonly string _filterText;

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

        internal bool HasDifferentFilterText => _filterText != null;

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
        /// </summary>
        public TextSpan Span { get; internal set; }

        /// <summary>
        /// Additional information attached to a completion item by it creator.
        /// </summary>
        public ImmutableDictionary<string, string> Properties { get; }

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
        /// The name of the <see cref="CompletionProvider"/> that created this 
        /// <see cref="CompletionItem"/>. Not available to clients. Only used by 
        /// the Completion subsystem itself for things like getting description text
        /// and making additional change during commit.
        /// </summary>
        internal string ProviderName { get; set; }

        /// <summary>
        /// The automation text to use when narrating the completion item. If set to
        /// null, narration will use the <see cref="DisplayText"/> instead.
        /// </summary>
        internal string AutomationText { get; set; }

        internal CompletionItemFlags Flags { get; set; }

        private CompletionItem(
            string displayText,
            string filterText,
            string sortText,
            TextSpan span,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules,
            string displayTextPrefix,
            string displayTextSuffix,
            string inlineDescription)
        {
            DisplayText = displayText ?? "";
            DisplayTextPrefix = displayTextPrefix ?? "";
            DisplayTextSuffix = displayTextSuffix ?? "";
            SortText = sortText ?? DisplayText;
            InlineDescription = inlineDescription ?? "";
            Span = span;
            Properties = properties ?? ImmutableDictionary<string, string>.Empty;
            Tags = tags.NullToEmpty();
            Rules = rules ?? CompletionItemRules.Default;

            if (!DisplayText.Equals(filterText, StringComparison.Ordinal))
            {
                _filterText = filterText;
            }
        }

        // binary back compat overload
        public static CompletionItem Create(
            string displayText,
            string filterText,
            string sortText,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules)
        {
            return Create(displayText, filterText, sortText, properties, tags, rules, displayTextPrefix: null, displayTextSuffix: null);
        }

        // binary back compat overload
        public static CompletionItem Create(
            string displayText,
            string filterText,
            string sortText,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules,
            string displayTextPrefix,
            string displayTextSuffix)
        {
            return Create(displayText, filterText, sortText, properties, tags, rules, displayTextPrefix, displayTextSuffix, inlineDescription: null);
        }

        public static CompletionItem Create(
            string displayText,
            string filterText = null,
            string sortText = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default,
            CompletionItemRules rules = null,
            string displayTextPrefix = null,
            string displayTextSuffix = null,
            string inlineDescription = null)
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
                inlineDescription: inlineDescription);
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
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules)
        {
            return new CompletionItem(
                span: span,
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                properties: properties,
                tags: tags,
                rules: rules,
                displayTextPrefix: null,
                displayTextSuffix: null,
                inlineDescription: null);
        }

        private CompletionItem With(
            Optional<TextSpan> span = default,
            Optional<string> displayText = default,
            Optional<string> filterText = default,
            Optional<string> sortText = default,
            Optional<ImmutableDictionary<string, string>> properties = default,
            Optional<ImmutableArray<string>> tags = default,
            Optional<CompletionItemRules> rules = default,
            Optional<string> displayTextPrefix = default,
            Optional<string> displayTextSuffix = default,
            Optional<string> inlineDescription = default)
        {
            var newSpan = span.HasValue ? span.Value : Span;
            var newDisplayText = displayText.HasValue ? displayText.Value : DisplayText;
            var newFilterText = filterText.HasValue ? filterText.Value : FilterText;
            var newSortText = sortText.HasValue ? sortText.Value : SortText;
            var newInlineDescription = inlineDescription.HasValue ? inlineDescription.Value : InlineDescription;
            var newProperties = properties.HasValue ? properties.Value : Properties;
            var newTags = tags.HasValue ? tags.Value : Tags;
            var newRules = rules.HasValue ? rules.Value : Rules;
            var newDisplayTextPrefix = displayTextPrefix.HasValue ? displayTextPrefix.Value : DisplayTextPrefix;
            var newDisplayTextSuffix = displayTextSuffix.HasValue ? displayTextSuffix.Value : DisplayTextSuffix;

            if (newSpan == Span &&
                newDisplayText == DisplayText &&
                newFilterText == FilterText &&
                newSortText == SortText &&
                newProperties == Properties &&
                newTags == Tags &&
                newRules == Rules &&
                newDisplayTextPrefix == DisplayTextPrefix &&
                newDisplayTextSuffix == DisplayTextSuffix &&
                newInlineDescription == InlineDescription)
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
                inlineDescription: newInlineDescription)
            {
                AutomationText = AutomationText,
                ProviderName = ProviderName,
                Flags = Flags,
            };
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Span"/> property changed.
        /// </summary>
        [Obsolete("Not used anymore.  CompletionList.Span is used to control the span used for filtering.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CompletionItem WithSpan(TextSpan span)
        {
            return this;
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="DisplayText"/> property changed.
        /// </summary>
        public CompletionItem WithDisplayText(string text)
        {
            return With(displayText: text);
        }

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
        {
            return With(filterText: text);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="SortText"/> property changed.
        /// </summary>
        public CompletionItem WithSortText(string text)
        {
            return With(sortText: text);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Properties"/> property changed.
        /// </summary>
        public CompletionItem WithProperties(ImmutableDictionary<string, string> properties)
        {
            return With(properties: properties);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with a property added to the <see cref="Properties"/> collection.
        /// </summary>
        public CompletionItem AddProperty(string name, string value)
        {
            return With(properties: Properties.Add(name, value));
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Tags"/> property changed.
        /// </summary>
        public CompletionItem WithTags(ImmutableArray<string> tags)
        {
            return With(tags: tags);
        }

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
        {
            return With(rules: rules);
        }

        private string _entireDisplayText;

        int IComparable<CompletionItem>.CompareTo(CompletionItem other)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(SortText, other.SortText);
            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(GetEntireDisplayText(), other.GetEntireDisplayText());
            }

            return result;
        }

        internal string GetEntireDisplayText()
        {
            if (_entireDisplayText == null)
            {
                _entireDisplayText = DisplayTextPrefix + DisplayText + DisplayTextSuffix;
            }

            return _entireDisplayText;
        }

        public override string ToString() => GetEntireDisplayText();
    }
}
