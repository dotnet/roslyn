// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The context presented to a <see cref="CompletionProvider"/> when providing completions.
    /// </summary>
    public sealed class CompletionContext
    {
        private readonly List<CompletionItem> _items;

        internal IReadOnlyList<CompletionItem> Items
        {
            get { return _items; }
        }

        internal CompletionProvider Provider { get; }

        /// <summary>
        /// The document that completion was invoked within.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The caret position when completion was triggered.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The span of the syntax element at the caret position.
        /// 
        /// This is the most common value used for <see cref="CompletionItem.Span"/> and will
        /// be automatically assigned to any <see cref="CompletionItem"/> that has no <see cref="CompletionItem.Span"/> specified.
        /// </summary>
        public TextSpan DefaultItemSpan { get; }

        /// <summary>
        /// The triggering action that caused completion to be started.
        /// </summary>
        public CompletionTrigger Trigger { get; }

        /// <summary>
        /// The options that completion was started with.
        /// </summary>
        public OptionSet Options { get; }

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Set to true if the items added here should be the only items presented to the user.
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        /// Creates a <see cref="CompletionContext"/> instance.
        /// </summary>
        public CompletionContext(
            CompletionProvider provider,
            Document document,
            int position,
            TextSpan defaultSpan,
            CompletionTrigger trigger,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (options == null)
            {
                throw new ArgumentException(nameof(options));
            }

            this.Provider = provider;
            this.Document = document;
            this.Position = position;
            this.DefaultItemSpan = defaultSpan;
            this.Trigger = trigger;
            this.Options = options;
            this.CancellationToken = cancellationToken;
            _items = new List<CompletionItem>();
        }

        public void AddItem(CompletionItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item = FixItem(item);
            _items.Add(item);
        }

        public void AddItems(IEnumerable<CompletionItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        private CompletionItem _suggestionModeItem;

        /// <summary>
        /// An optional <see cref="CompletionItem"/> that appears selected in the list presented to the user during suggestion mode.
        /// 
        /// Suggestion mode disables autoselection of items in the list, giving preference to the text typed by the user unless a specific item is selected manually.
        /// 
        /// Specifying a <see cref="SuggestionModeItem"/> is a request that the completion host operate in suggestion mode.
        /// The item specified determines the text displayed and the description associated with it unless a different item is manually selected.
        /// 
        /// No text is ever inserted when this item is completed, leaving the text the user typed instead.
        /// </summary>
        public CompletionItem SuggestionModeItem
        {
            get
            {
                return _suggestionModeItem;
            }

            set
            {
                _suggestionModeItem = value;

                if (_suggestionModeItem != null)
                {
                    _suggestionModeItem = FixItem(_suggestionModeItem);
                }
            }
        }

        private CompletionItem FixItem(CompletionItem item)
        {
            // remember provider so we can find it again later
            item = item.AddProperty("Provider", this.Provider.Name);

            // assign the default span if not set by provider
            if (item.Span == default(TextSpan) && this.DefaultItemSpan != default(TextSpan))
            {
                item = item.WithSpan(this.DefaultItemSpan);
            }

            return item;
        }
    }
}
