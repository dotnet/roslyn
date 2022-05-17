// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetSource : IAssetSource
    {
        private readonly ServiceBrokerClient _client;

        public SolutionAssetSource(ServiceBrokerClient client)
        {
            _client = client;
        }

        public async ValueTask<ImmutableArray<(Checksum, object)>> GetAssetsAsync(Checksum solutionChecksum, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
            await TaskScheduler.Default;

            using var provider = await _client.GetProxyAsync<ISolutionAssetProvider>(SolutionAssetProvider.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(provider.Proxy);

            return await new RemoteCallback<ISolutionAssetProvider>(provider.Proxy).InvokeAsync(
                (proxy, pipeWriter, cancellationToken) => proxy.GetAssetsAsync(pipeWriter, solutionChecksum, checksums.ToArray(), cancellationToken),
                (pipeReader, cancellationToken) => RemoteHostAssetSerialization.ReadDataAsync(pipeReader, solutionChecksum, checksums, serializerService, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
