// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class SolutionAssetSource(ServiceBrokerClient client) : IAssetSource
{
    private readonly ServiceBrokerClient _client = client;

    public async ValueTask<ImmutableArray<object>> GetAssetsAsync(
        Checksum solutionChecksum,
        AssetHint assetHint,
        ImmutableArray<Checksum> checksums,
        ISerializerService serializerService,
        CancellationToken cancellationToken)
    {
        // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
        await TaskScheduler.Default;

        return await RemoteCallback<ISolutionAssetProvider>.InvokeServiceAsync(
            _client,
            SolutionAssetProvider.ServiceDescriptor,
            (callback, cancellationToken) => callback.InvokeAsync(
                (proxy, pipeWriter, cancellationToken) => proxy.WriteAssetsAsync(pipeWriter, solutionChecksum, assetHint, checksums, cancellationToken),
                (pipeReader, cancellationToken) => RemoteHostAssetSerialization.ReadDataAsync(pipeReader, solutionChecksum, checksums.Length, serializerService, cancellationToken),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
