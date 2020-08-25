// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetProvider : ISolutionAssetProvider
    {
        public const string ServiceName = "RoslynSolutionAssetProvider";

        internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceName);

        private readonly HostWorkspaceServices _services;

        public SolutionAssetProvider(HostWorkspaceServices services)
        {
            _services = services;
        }

        public Task GetAssetsAsync(Stream outputStream, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            using var writer = new ObjectWriter(outputStream, leaveOpen: false, cancellationToken);

            var assetStorage = _services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            var serializer = _services.GetRequiredService<ISerializerService>();

            // Complete RPC right away so the client can start reading from the stream.
            _ = Task.Run(() => RemoteHostAssetSerialization.WriteDataAsync(writer, assetStorage, serializer, scopeId, checksums, cancellationToken), cancellationToken);

            return Task.CompletedTask;
        }

        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
            => Task.FromResult(_services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName));
    }
}
