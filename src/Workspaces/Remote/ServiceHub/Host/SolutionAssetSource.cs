// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class SolutionAssetSource(ServiceBrokerClient client) : IAssetSource
{
    private readonly ServiceBrokerClient _client = client;

    public async ValueTask GetAssetsAsync<T, TArg>(
        Checksum solutionChecksum,
        AssetPath assetPath,
        ReadOnlyMemory<Checksum> checksums,
        ISerializerService serializerService,
        Action<Checksum, T, TArg> assetCallback,
        TArg arg,
        CancellationToken cancellationToken)
    {
        // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
        await TaskScheduler.Default;

        await RemoteCallback<ISolutionAssetProvider>.InvokeServiceAsync(
            _client,
            SolutionAssetProvider.ServiceDescriptor,
            (callback, cancellationToken) => callback.InvokeAsync(
                (proxy, pipeWriter, cancellationToken) => proxy.WriteAssetsAsync(pipeWriter, solutionChecksum, assetPath, checksums, cancellationToken),
                (pipeReader, cancellationToken) => new RemoteHostAssetReader<T, TArg>(pipeReader, solutionChecksum, checksums.Length, serializerService, assetCallback, arg).ReadDataAsync(cancellationToken),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
