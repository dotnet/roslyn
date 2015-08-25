// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal class MostRecentlyUsedList
    {
        private const int DefaultSize = 10;

        private readonly List<string> _items = new List<string>(DefaultSize);
        private readonly object _gate = new object();

        public void AddItem(CompletionItem item)
        {
            lock (_gate)
            {
                // We need to remove the item if it's already in the list.
                // If we're at capacity, we need to remove the LRU item.
                var removed = _items.Remove(item.DisplayText);
                if (!removed && _items.Count == DefaultSize)
                {
                    _items.RemoveAt(0);
                }

                _items.Add(item.DisplayText);
            }
        }

        public bool Contains(CompletionItem item)
        {
            return _items.Contains(item.DisplayText);
        }

        internal int GetMRUIndex(CompletionItem item)
        {
            lock (_gate)
            {
                // A lower value indicates more recently used.  Since items are added
                // to the end of the list, our result just maps to the negation of the 
                // index.
                // -1 => 1  == Not Found
                // 0  => 0  == least recently used 
                // 9  => -9 == most recently used 
                var index = _items.IndexOf(item.DisplayText);
                return -index;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _items.Clear();
            }
        }
    }
}
