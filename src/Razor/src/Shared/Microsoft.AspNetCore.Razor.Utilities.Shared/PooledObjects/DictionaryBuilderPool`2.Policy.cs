// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class DictionaryBuilderPool<TKey, TValue>
{
    private sealed class Policy : PooledObjectPolicy
    {
        public static readonly Policy Default = new(keyComparer: null);

        private readonly IEqualityComparer<TKey>? _keyComparer;

        private Policy(IEqualityComparer<TKey>? keyComparer)
        {
            _keyComparer = keyComparer;
        }

        public static Policy Create(Optional<IEqualityComparer<TKey>?> keyComparer = default)
        {
            if (!keyComparer.HasValue || keyComparer.Value == Default._keyComparer)
            {
                return Default;
            }

            return new(keyComparer.GetValueOrDefault(null));
        }

        public override ImmutableDictionary<TKey, TValue>.Builder Create()
            => ImmutableDictionary.CreateBuilder<TKey, TValue>(_keyComparer);

        public override bool Return(ImmutableDictionary<TKey, TValue>.Builder builder)
        {
            builder.Clear();

            return true;
        }
    }
}
