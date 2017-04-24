// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal class GoToDefinitionContext
    {
        private readonly object _gate = new object();

        private readonly Dictionary<string, List<DefinitionItem>> _items = new Dictionary<string, List<DefinitionItem>>();

        public GoToDefinitionContext(Document document, int position, CancellationToken cancellationToken)
        {
            Document = document;
            Position = position;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public int Position { get; }
        public CancellationToken CancellationToken { get; }

        public IReadOnlyDictionary<string, List<DefinitionItem>> Items => _items;
        public TextSpan Span { get; set; }

        public void AddItem(string key, DefinitionItem item)
        {
            lock (_gate)
            {
                if (!_items.ContainsKey(key))
                {
                    _items[key] = new List<DefinitionItem>();
                }

                _items[key].Add(item);
            }
        }
    }
}
