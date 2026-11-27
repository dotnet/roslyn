// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.PooledObjects;

internal static class TestArrayBuilderPool<T>
{
    public static ArrayBuilderPool<T> Create(
        ArrayBuilderPool<T>.PooledObjectPolicy? policy = null, int size = 1)
        => ArrayBuilderPool<T>.Create(policy ?? NoReturnPolicy.Instance, size);

    public sealed class NoReturnPolicy : ArrayBuilderPool<T>.PooledObjectPolicy
    {
        public static readonly NoReturnPolicy Instance = new();

        private NoReturnPolicy()
        {
        }

        public override ImmutableArray<T>.Builder Create()
            => ImmutableArray.CreateBuilder<T>();

        public override bool Return(ImmutableArray<T>.Builder obj)
            => false;
    }
}
