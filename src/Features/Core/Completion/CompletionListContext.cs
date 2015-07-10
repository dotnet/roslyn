// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Completion
{
    internal struct CompletionListContext
    {
        private readonly Document _document;
        private readonly int _position;
        private readonly CompletionTriggerInfo _triggerInfo;
        private readonly CancellationToken _cancellationToken;

        private readonly Action<CompletionItem> _addCompletionItem;
        private readonly Action<CompletionItem> _registerBuilder;
        private readonly Action<bool> _makeExclusive;

        public Document Document => _document;
        public int Position => _position;
        public CompletionTriggerInfo TriggerInfo => _triggerInfo;
        public CancellationToken CancellationToken => _cancellationToken;

        public CompletionListContext(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            Action<CompletionItem> addCompletionItem,
            Action<CompletionItem> registerBuilder,
            Action<bool> makeExclusive,
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (addCompletionItem == null)
            {
                throw new ArgumentNullException(nameof(addCompletionItem));
            }

            if (registerBuilder == null)
            {
                throw new ArgumentNullException(nameof(registerBuilder));
            }

            if (makeExclusive == null)
            {
                throw new ArgumentNullException(nameof(makeExclusive));
            }

            _document = document;
            _position = position;
            _triggerInfo = triggerInfo;
            _addCompletionItem = addCompletionItem;
            _registerBuilder = registerBuilder;
            _makeExclusive = makeExclusive;
            _cancellationToken = cancellationToken;
        }

        public void AddCompletionItem(CompletionItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _addCompletionItem(item);
        }

        public void RegisterBuilder(CompletionItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _registerBuilder(item);
        }

        public void MakeExclusive(bool value)
        {
            _makeExclusive(value);
        }
    }
}
