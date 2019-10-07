// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Dictionary<TKey, TValue> : Collection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
                where TKey : notnull
            {
                public static readonly new Dictionary<TKey, TValue> Instance = new Dictionary<TKey, TValue>();

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
                    value = default!;
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
