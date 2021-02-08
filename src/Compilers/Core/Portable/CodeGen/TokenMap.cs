// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Cci;

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
        private readonly ConcurrentDictionary<IReferenceOrISignature, uint> _itemIdentityToToken = new();
        private object[] _items = Array.Empty<object>();
        private int _count = 0;

        internal TokenMap() { }

        public uint GetOrAddTokenFor(IReference item, out bool referenceAdded)
        {
            if (_itemIdentityToToken.TryGetValue(new IReferenceOrISignature(item), out uint token))
            {
                referenceAdded = false;
                return token;
            }

            return AddItem(new IReferenceOrISignature(item), out referenceAdded);
        }

        public uint GetOrAddTokenFor(ISignature item, out bool referenceAdded)
        {
            if (_itemIdentityToToken.TryGetValue(new IReferenceOrISignature(item), out uint token))
            {
                referenceAdded = false;
                return token;
            }

            return AddItem(new IReferenceOrISignature(item), out referenceAdded);
        }

        private uint AddItem(IReferenceOrISignature item, out bool referenceAdded)
        {
            uint token;
            // NOTE: cannot use GetOrAdd here since items and itemToToken must be in sync
            // so if we do need to add we have to take a lock and modify both collections.
            lock (_itemIdentityToToken)
            {
                if (!_itemIdentityToToken.TryGetValue(item, out token))
                {
                    token = (uint)_count;
                    // Add the token for this type
                    referenceAdded = _itemIdentityToToken.TryAdd(item, token);
                    Debug.Assert(referenceAdded);

                    var count = (int)token + 1;
                    var items = _items;
                    if (items.Length > count)
                    {
                        items[(int)token] = item.AsObject();
                    }
                    else
                    {
                        // Not enough room, we need to resize the array
                        Array.Resize(ref items, Math.Max(8, count * 2));
                        items[(int)token] = item.AsObject();

                        // Update the updated array reference before updating _count
                        Volatile.Write(ref _items, items);
                    }

                    Volatile.Write(ref _count, count);
                }
                else
                {
                    referenceAdded = false;
                }
            }

            return token;
        }

        public object GetItem(uint token)
        {
            // If a token has been handed out, then it should be always within _count of the
            // current array and a lock is not required.
            Debug.Assert(token < (uint)_count && _count <= _items.Length);

            return _items[(int)token];
        }

        //TODO: why is this is called twice during emit?
        //      should probably return ROA instead of IE and cache that in Module. (and no need to return count)
        public ReadOnlySpan<object> GetAllItems()
        {
            // Read _count before _items reference, to match inverse of the writes in AddItem.
            // So _items is guaranteed to have at least count items; and a lock is not required.

            // Read the count prior to getting the array
            int count = Volatile.Read(ref _count);
            // Read the array reference
            object[] items = Volatile.Read(ref _items);

            // Return a right sized view of the array based on read count and reference.
            return new ReadOnlySpan<object>(items, 0, count);
        }
    }
}
