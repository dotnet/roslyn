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
    /// <summary>
    /// Clone of the platform's Microsoft.VisualStudio.RpcContracts.Caching.ICacheService, but defined so we can
    /// reference it in the workspace layer, and function if it is not present.
    /// </summary>
    internal interface IRoslynCloudCacheService : IDisposable, IAsyncDisposable
    {
        Task<bool> CheckExistsAsync(RoslynCloudCacheItemKey key, CancellationToken cancellationToken);
        ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken);
        Task SetItemAsync(RoslynCloudCacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken);
        Task<bool> TryGetItemAsync(RoslynCloudCacheItemKey key, PipeWriter writer, CancellationToken cancellationToken);
    }

    internal readonly struct RoslynCloudCacheItemKey
    {
        public readonly RoslynCloudCacheContainerKey ContainerKey;
        public readonly string ItemName;
        public readonly ReadOnlyMemory<byte> Version;

        public RoslynCloudCacheItemKey(RoslynCloudCacheContainerKey containerKey, string itemName, ReadOnlyMemory<byte> version = default)
        {
            ContainerKey = containerKey;
            ItemName = itemName;
            Version = version;
        }
    }

    internal readonly struct RoslynCloudCacheContainerKey
    {
        /// <remarks>
        /// We use <see cref="StringComparer.Ordinal"/> here to match what the platform does internally.  If we do this
        /// they do not need to copy the values from us to them and can instead just point directly at our dictionary
        /// instance.
        /// </remarks>
        private static readonly ImmutableSortedDictionary<string, string?> s_empty = ImmutableSortedDictionary.Create<string, string?>(StringComparer.Ordinal);

        public readonly string Component;
        public readonly ImmutableSortedDictionary<string, string?> Dimensions;

        public RoslynCloudCacheContainerKey(string component, ImmutableSortedDictionary<string, string?>? dimensions = null)
        {
            Component = component;
            Dimensions = dimensions ?? s_empty;
        }
    }
}
