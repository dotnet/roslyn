// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionListContext
    {
        private readonly ImmutableArray<CompletionItem>.Builder _itemsBuilder;
        private CompletionItem _builder;
        private bool _isExclusive;

        public Document Document { get; }
        public int Position { get; }
        public CompletionTriggerInfo TriggerInfo { get; }
        public OptionSet Options { get; }
        public CancellationToken CancellationToken { get; }

        public CompletionItem Builder => _builder;
        public bool IsExclusive => _isExclusive;

        public CompletionListContext(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            this.Document = document;
            this.Position = position;
            this.TriggerInfo = triggerInfo;
            this.Options = options;
            this.CancellationToken = cancellationToken;

            _itemsBuilder = ImmutableArray.CreateBuilder<CompletionItem>();
        }

        public void AddItem(CompletionItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _itemsBuilder.Add(item);
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

        public ImmutableArray<CompletionItem> GetItems()
        {
            return _itemsBuilder.AsImmutable();
        }

        public void RegisterBuilder(CompletionItem builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (_builder != null)
            {
                throw new InvalidOperationException("Builder has already been registered.");
            }

            _builder = builder;
        }

        public void MakeExclusive(bool value)
        {
            _isExclusive = value;
        }
    }
}
