// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetSource : IAssetSource
    {
        private readonly ServiceBrokerClient _client;
        private readonly CancellationTokenSource _clientDisconnectedSource;

        public SolutionAssetSource(ServiceBrokerClient client, CancellationTokenSource clientDisconnectedSource)
        {
            _client = client;
            _clientDisconnectedSource = clientDisconnectedSource;
        }

        public async ValueTask<ImmutableArray<(Checksum, object)>> GetAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var provider = await _client.GetProxyAsync<ISolutionAssetProvider>(SolutionAssetProvider.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(provider.Proxy);

            return await new RemoteCallback<ISolutionAssetProvider>(provider.Proxy, _clientDisconnectedSource).InvokeAsync(
                (proxy, stream, cancellationToken) => proxy.GetAssetsAsync(stream, scopeId, checksums.ToArray(), cancellationToken),
                (stream, cancellationToken) => RemoteHostAssetSerialization.ReadDataAsync(stream, scopeId, checksums, serializerService, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            using var provider = await _client.GetProxyAsync<ISolutionAssetProvider>(SolutionAssetProvider.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(provider.Proxy);

            return await new RemoteCallback<ISolutionAssetProvider>(provider.Proxy, _clientDisconnectedSource).InvokeAsync(
                (self, cancellationToken) => provider.Proxy.IsExperimentEnabledAsync(experimentName, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
