// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal class GoToDefinitionContext
    {
        private readonly object _gate = new object();

        private readonly MultiDictionary<string, DefinitionItem> _items = new MultiDictionary<string, DefinitionItem>();

        public GoToDefinitionContext(Document document, int position, CancellationToken cancellationToken)
        {
            Document = document;
            Position = position;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public int Position { get; }
        public CancellationToken CancellationToken { get; }

        public TextSpan Span { get; set; }

        public bool TryGetItems(string key, out IEnumerable<DefinitionItem> items)
        {
            items = _items[key];
            return items != null;
        }

        public void AddItem(string key, DefinitionItem item)
        {
            lock (_gate)
            {
                _items.Add(key, item);
            }
        }
    }
}
