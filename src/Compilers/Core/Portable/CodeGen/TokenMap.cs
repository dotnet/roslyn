// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// This map supports tokens of type <see cref="Cci.ISignature"/> and <see cref="Cci.IReference"/>.
    /// </summary>
    internal sealed class TokenMap
    {
        private readonly ConcurrentDictionary<object, uint> _itemIdentityToToken = new ConcurrentDictionary<object, uint>(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<object, uint> _itemToToken;
        private readonly ArrayBuilder<object> _items = new ArrayBuilder<object>();

        internal TokenMap(IEqualityComparer<object> comparer)
        {
            _itemToToken = new Dictionary<object, uint>(comparer);
        }

        public uint GetOrAddTokenFor(object item, out bool referenceAdded)
        {
            uint tmp;
            if (_itemIdentityToToken.TryGetValue(item, out tmp))
            {
                referenceAdded = false;
                return (uint)tmp;
            }

            return AddItem(item, out referenceAdded);
        }

        private uint AddItem(object item, out bool referenceAdded)
        {
            Debug.Assert(item is Cci.ISignature || item is Cci.IReference);
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

        public object GetItem(uint token)
        {
            lock (_items)
            {
                return _items[(int)token];
            }
        }

        public IEnumerable<object> GetAllItems()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }

        //TODO: why is this is called twice during emit?
        //      should probably return ROA instead of IE and cache that in Module. (and no need to return count)
        public IEnumerable<object> GetAllItemsAndCount(out int count)
        {
            lock (_items)
            {
                count = _items.Count;
                return _items.ToArray();
            }
        }
    }
}
