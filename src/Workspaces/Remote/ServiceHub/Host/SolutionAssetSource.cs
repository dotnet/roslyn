// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

        public SolutionAssetSource(ServiceBrokerClient client)
        {
            _client = client;
        }

        public async Task<ImmutableArray<(Checksum, object)>> GetAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var provider = await _client.GetProxyAsync<ISolutionAssetProvider>(SolutionAssetProvider.ServiceDescriptor).ConfigureAwait(false);
            Contract.ThrowIfNull(provider.Proxy);

            using var stream = new MemoryStream();
            await provider.Proxy.GetAssetsAsync(stream, scopeId, checksums.ToArray(), cancellationToken).ConfigureAwait(false);
            return RemoteHostAssetSerialization.ReadData(stream, scopeId, checksums, serializerService, cancellationToken);
        }

        public async Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            using var provider = await _client.GetProxyAsync<ISolutionAssetProvider>(SolutionAssetProvider.ServiceDescriptor).ConfigureAwait(false);
            Contract.ThrowIfNull(provider.Proxy);

            return await provider.Proxy.IsExperimentEnabledAsync(experimentName, cancellationToken).ConfigureAwait(false);
        }
    }
}
