// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetProvider : ISolutionAssetProvider
    {
        internal static ServiceRpcDescriptor ServiceDescriptor { get; } = new ServiceJsonRpcDescriptor(
            new ServiceMoniker(new RemoteServiceName("RoslynSolutionAssetProvider").ToString(isRemoteHost64Bit: true)),
            clientInterface: null,
            ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader);

        private readonly SolutionAssetStorage _assetStorage;
        private readonly ISerializerService _serializer;

        public SolutionAssetProvider(HostWorkspaceServices services)
        {
            _assetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            _serializer = services.GetRequiredService<ISerializerService>();
        }

        public Task GetAssetsAsync(Stream outputStream, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            using var writer = new ObjectWriter(outputStream, leaveOpen: false, cancellationToken);

            // Complete client RPC right away so it can start reading from the stream.
            _ = Task.Run(() => RemoteHostAssetSerialization.WriteDataAsync(writer, _assetStorage, _serializer, scopeId, checksums, cancellationToken), cancellationToken);

            return Task.CompletedTask;
        }

        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
