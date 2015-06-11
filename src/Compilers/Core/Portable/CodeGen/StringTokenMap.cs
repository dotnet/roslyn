// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Handles storage of strings referenced via tokens in metadata. When items are stored 
    /// they are uniquely "associated" with fake token, which is basically a sequential number.
    /// IL gen will use these fake tokens during codegen and later, when actual token values 
    /// are known the method bodies will be patched.
    /// To support these two scenarios we need two maps - Item-->uint, and uint-->Item.  (the second is really just a list).
    /// </summary>
    internal sealed class StringTokenMap
    {
        private readonly ConcurrentDictionary<string, uint> _itemToToken = new ConcurrentDictionary<string, uint>(ReferenceEqualityComparer.Instance);
        private readonly ArrayBuilder<string> _items = new ArrayBuilder<string>();

        public uint GetOrAddTokenFor(string item)
        {
            uint token;
            // NOTE: cannot use GetOrAdd here since items and itemToToken must be in sync
            // so if we do need to add we have to take a lock and modify both collections.
            if (_itemToToken.TryGetValue(item, out token))
            {
                return token;
            }

            return AddItem(item);
        }

        private uint AddItem(string item)
        {
            uint token;

            lock (_items)
            {
                if (_itemToToken.TryGetValue(item, out token))
                {
                    return token;
                }

                token = (uint)_items.Count;
                _items.Add(item);
                _itemToToken.Add(item, token);
            }

            return token;
        }

        public string GetItem(uint token)
        {
            lock (_items)
            {
                return _items[(int)token];
            }
        }

        public IEnumerable<string> GetAllItems()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }

        //TODO: why is this is called twice during emit?
        //      should probably return ROA instead of IE and cache that in Module. (and no need to return count)
        public IEnumerable<string> GetAllItemsAndCount(out int count)
        {
            lock (_items)
            {
                count = _items.Count;
                return _items.ToArray();
            }
        }
    }
}
