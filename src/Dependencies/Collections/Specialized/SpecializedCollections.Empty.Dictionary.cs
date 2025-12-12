// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Dictionary<TKey, TValue>
#nullable disable
                // Note: if the interfaces we implement weren't oblivious, then we'd warn about the `[MaybeNullWhen(false)] out TValue value` parameter below
                // We can remove this once `IDictionary` is annotated with `[MaybeNullWhen(false)]`
                : Collection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
#nullable enable
                where TKey : notnull
            {
                public static new readonly Dictionary<TKey, TValue> Instance = new();

                private Dictionary()
                {
                }

                public void Add(TKey key, TValue value)
                {
                    throw new NotSupportedException();
                }

                public bool ContainsKey(TKey key)
                {
                    return false;
                }

                public ICollection<TKey> Keys
                {
                    get
                    {
                        return Collection<TKey>.Instance;
                    }
                }

                IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
                IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

                public bool Remove(TKey key)
                {
                    throw new NotSupportedException();
                }

                public bool TryGetValue(TKey key, [MaybeNullWhen(returnValue: false)] out TValue value)
                {
                    value = default;
                    return false;
                }

                public ICollection<TValue> Values
                {
                    get
                    {
                        return Collection<TValue>.Instance;
                    }
                }

                public TValue this[TKey key]
                {
                    get
                    {
                        throw new NotSupportedException();
                    }

                    set
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
    }
}
