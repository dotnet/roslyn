// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal sealed class SignatureContext
    {
        private readonly List<SignatureHelpItem> _items;
        private TextSpan _applicableSpan;
        private SignatureHelpState _state;

        internal IReadOnlyList<SignatureHelpItem> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// The document that Signature Help was invoked within.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The caret position when Signature Help was triggered.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The triggering action that caused Signature Help to be started.
        /// </summary>
        public SignatureHelpTrigger Trigger { get; }

        /// <summary>
        /// The options that Signature Help was started with.
        /// </summary>
        public OptionSet Options { get; }

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        public TextSpan ApplicableSpan => _applicableSpan;

        public SignatureHelpState State => _state;


        private readonly SignatureHelpProvider _provider;

        /// <summary>
        /// Creates a <see cref="SignatureContext"/> instance.
        /// </summary>
        internal SignatureContext(
            SignatureHelpProvider provider,
            Document document,
            int position,
            SignatureHelpTrigger trigger,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _provider = provider;
            this.Document = document;
            this.Position = position;
            this.Trigger = trigger;
            this.Options = options;
            this.CancellationToken = cancellationToken;
            _items = new List<SignatureHelpItem>();
        }

        public void AddItem(SignatureHelpItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // associate item and parameters with this provider
            item = item.WithProperties(item.Properties.SetItem("Provider", _provider.Name));

            if (item.Parameters.Length > 0)
            {
                item = item.WithParameters(item.Parameters.Select(p => p.WithProperties(p.Properties.SetItem("Provider", _provider.Name))).ToImmutableArray());
            }

            _items.Add(item);
        }

        public void AddItems(IEnumerable<SignatureHelpItem> items)
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

        public void SetSpan(TextSpan span)
        {
            _applicableSpan = span;
        }

        public void SetState(SignatureHelpState state)
        {
            _state = state;
        }

        internal SignatureList ToSignatureList()
        {
            if (_items == null || !_items.Any() || _state == null)
            {
                return SignatureList.Empty;
            }

            var items = Filter(_items.Distinct(), _state.ArgumentNames);
            return new SignatureList(items, _applicableSpan, _state.ArgumentIndex, _state.ArgumentCount, _state.ArgumentName);
        }

        private static ImmutableArray<SignatureHelpItem> Filter(IEnumerable<SignatureHelpItem> items, IEnumerable<string> parameterNames)
        {
            if (parameterNames == null)
            {
                return items.ToImmutableArray();
            }

            var filteredList = items.Where(i => Include(i, parameterNames)).ToImmutableArray();
            return filteredList.Length == 0 ? items.ToImmutableArray() : filteredList;
        }

        private static bool Include(SignatureHelpItem item, IEnumerable<string> parameterNames)
        {
            var itemParameterNames = item.Parameters.Select(p => p.Name).ToSet();
            return parameterNames.All(itemParameterNames.Contains);
        }

        internal void UpdateItems(IEnumerable<SignatureHelpItem> finalItems)
        {
            _items.Clear();
            _items.AddRange(finalItems);
        }
    }
}
