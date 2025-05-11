// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Handles storage of items referenced via tokens in metadata. When items are stored
    /// they are uniquely "associated" with fake tokens, which are basically sequential numbers.
    /// IL gen will use these fake tokens during codegen and later, when actual values
    /// are known, the method bodies will be patched.
    /// To support these two scenarios we need two maps - Item-->uint, and uint-->Item. (The second is really just a list).
    /// </summary>
    internal sealed class ItemTokenMap<T> where T : class
    {
        private readonly ConcurrentDictionary<T, uint> _itemToToken = new ConcurrentDictionary<T, uint>(ReferenceEqualityComparer.Instance);
        private readonly ArrayBuilder<T> _items = new ArrayBuilder<T>();

        public uint GetOrAddTokenFor(T item)
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

        private uint AddItem(T item)
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

        public T GetItem(uint token)
        {
            lock (_items)
            {
                return _items[(int)token];
            }
        }

        public T[] CopyItems()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }
    }
}
