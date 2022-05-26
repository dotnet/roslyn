// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal class GoToSymbolContext
    {
        private readonly object _gate = new();

        private readonly MultiDictionary<string, DefinitionItem> _items = new();

        public GoToSymbolContext(Document document, int position, CancellationToken cancellationToken)
        {
            Document = document;
            Position = position;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public int Position { get; }
        public CancellationToken CancellationToken { get; }

        public TextSpan Span { get; set; }

        internal bool TryGetItems(string key, out IEnumerable<DefinitionItem> items)
        {
            if (_items.ContainsKey(key))
            {
                // Multidictionary valuesets are structs so we can't
                // just check for null
                items = _items[key];
                return true;
            }
            else
            {
                items = null;
                return false;
            }
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
