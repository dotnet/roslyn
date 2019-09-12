// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Handles storage of items referenced via tokens in metadata (strings or Symbols).
    /// When items are stored they are uniquely "associated" with fake token, which is basically 
    /// a sequential number.
    /// IL gen will use these fake tokens during codegen and later, when actual token values are known
    /// the method bodies will be patched.
    /// To support these two scenarios we need two maps - Item-->uint, and uint-->Item.  (the second is really just a list).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class TokenMap<T> where T : class
    {
        private readonly ConcurrentDictionary<T, uint> _itemIdentityToToken = new ConcurrentDictionary<T, uint>(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<T, uint> _itemToToken;
        private readonly ArrayBuilder<T> _items = new ArrayBuilder<T>();

        internal TokenMap(IEqualityComparer<T> comparer)
        {
            _itemToToken = new Dictionary<T, uint>(comparer);
        }

        public uint GetOrAddTokenFor(T item, out bool referenceAdded)
        {
            uint tmp;
            if (_itemIdentityToToken.TryGetValue(item, out tmp))
            {
                referenceAdded = false;
                return (uint)tmp;
            }

            return AddItem(item, out referenceAdded);
        }

        private uint AddItem(T item, out bool referenceAdded)
        {
            uint token;

            // NOTE: cannot use GetOrAdd here since items and itemToToken must be in sync
            // so if we do need to add we have to take a lock and modify both collections.
            lock (_items)
            {
                if (!_itemToToken.TryGetValue(item, out token))
                {
                    token = (uint)_items.Count;
                    _items.Add(item);
                    _itemToToken.Add(item, token);
                }
            }

            referenceAdded = _itemIdentityToToken.TryAdd(item, token);
            return token;
        }

        public T GetItem(uint token)
        {
            lock (_items)
            {
                return _items[(int)token];
            }
        }

        public IEnumerable<T> GetAllItems()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }

        //TODO: why is this is called twice during emit?
        //      should probably return ROA instead of IE and cache that in Module. (and no need to return count)
        public IEnumerable<T> GetAllItemsAndCount(out int count)
        {
            lock (_items)
            {
                count = _items.Count;
                return _items.ToArray();
            }
        }
    }
}
