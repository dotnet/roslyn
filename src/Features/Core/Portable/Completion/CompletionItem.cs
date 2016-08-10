// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
        /// <summary>
        /// The text that is displayed to the user.
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// The text used to determine if the item matches the filter and is show in the list.
        /// This is often the same as <see cref="DisplayText"/> but may be different in certain circumstances.
        /// </summary>
        public string FilterText { get; }

        /// <summary>
        /// The text used to determine the order that the item appears in the list.
        /// This is often the same as the <see cref="DisplayText"/> but may be different in certain circumstances.
        /// </summary>
        public string SortText { get; }

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
        /// Descriptive tags from <see cref="CompletionTags"/>.
        /// These tags may influence how the item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// Rules that declare how this item should behave.
        /// </summary>
        public CompletionItemRules Rules { get; }

        private CompletionItem(
            string displayText,
            string filterText,
            string sortText,
            TextSpan span,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules)
        {
            this.DisplayText = displayText ?? "";
            this.FilterText = filterText ?? this.DisplayText;
            this.SortText = sortText ?? this.DisplayText;
            this.Span = span;
            this.Properties = properties ?? ImmutableDictionary<string, string>.Empty;
            this.Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;
            this.Rules = rules ?? CompletionItemRules.Default;
        }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
        public static CompletionItem Create(
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
            string displayText,
            string filterText = null,
            string sortText = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            return new CompletionItem(
                span: default(TextSpan),
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                properties: properties,
                tags: tags,
                rules: rules);
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
        [Obsolete("Use the Create overload that does not take a span")]
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
                rules: rules);
        }

        private CompletionItem With(
            Optional<TextSpan> span = default(Optional<TextSpan>),
            Optional<string> displayText = default(Optional<string>),
            Optional<string> filterText = default(Optional<string>),
            Optional<string> sortText = default(Optional<string>),
            Optional<ImmutableDictionary<string, string>> properties = default(Optional<ImmutableDictionary<string, string>>),
            Optional<ImmutableArray<string>> tags = default(Optional<ImmutableArray<string>>),
            Optional<CompletionItemRules> rules = default(Optional<CompletionItemRules>))
        {
            var newSpan = span.HasValue ? span.Value : this.Span;
            var newDisplayText = displayText.HasValue ? displayText.Value : this.DisplayText;
            var newFilterText = filterText.HasValue ? filterText.Value : this.FilterText;
            var newSortText = sortText.HasValue ? sortText.Value : this.SortText;
            var newProperties = properties.HasValue ? properties.Value : this.Properties;
            var newTags = tags.HasValue ? tags.Value : this.Tags;
            var newRules = rules.HasValue ? rules.Value : this.Rules;

            if (newSpan == this.Span &&
                newDisplayText == this.DisplayText &&
                newFilterText == this.FilterText &&
                newSortText == this.SortText &&
                newProperties == this.Properties &&
                newTags == this.Tags &&
                newRules == this.Rules)
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
                rules: newRules);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Span"/> property changed.
        /// </summary>
        [Obsolete("Not used anymore.  CompletionList.Span is used to control the span used for filtering.")]
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
            return With(properties: this.Properties.Add(name, value));
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

            if (this.Tags.Contains(tag))
            {
                return this;
            }
            else
            {
                return With(tags: this.Tags.Add(tag));
            }
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionItem"/> with the <see cref="Rules"/> property changed.
        /// </summary>
        public CompletionItem WithRules(CompletionItemRules rules)
        {
            return With(rules: rules);
        }

        int IComparable<CompletionItem>.CompareTo(CompletionItem other)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(this.SortText, other.SortText);
            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(this.DisplayText, other.DisplayText);
            }

            return result;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}