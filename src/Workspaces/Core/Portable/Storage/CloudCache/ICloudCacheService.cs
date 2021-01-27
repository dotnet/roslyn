// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Storage
{
    internal interface ICloudCacheService : IDisposable
    {
        Task<bool> CheckExistsAsync(CloudCacheItemKey key, CancellationToken cancellationToken);
        ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken);
        Task SetItemAsync(CloudCacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken);
        Task<bool> TryGetItemAsync(CloudCacheItemKey key, PipeWriter writer, CancellationToken cancellationToken);
    }

    internal readonly struct CloudCacheItemKey
    {
        public readonly CloudCacheContainerKey ContainerKey;
        public readonly string ItemName;
        public readonly ReadOnlyMemory<byte> Version;

        public CloudCacheItemKey(CloudCacheContainerKey containerKey, string itemName, ReadOnlyMemory<byte> version = default)
        {
            ContainerKey = containerKey;
            ItemName = itemName;
            Version = version;
        }
    }

    internal readonly struct CloudCacheContainerKey
    {
        public readonly string Component;
        public readonly ImmutableSortedDictionary<string, string?> Dimensions;

        public CloudCacheContainerKey(string component, ImmutableSortedDictionary<string, string?>? dimensions = null)
        {
            Component = component;
            Dimensions = dimensions ?? ImmutableSortedDictionary<string, string?>.Empty;
        }
    }
}
