// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class SolutionSnapshotService : ServiceHubJsonRpcServiceBase
    {
        private readonly AssetSource _source;

        public SolutionSnapshotService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
            _source = new ServiceHubAssetSource(Rpc, Logger, CancellationToken);
        }

        public void Done()
        {
            _source.Done();
        }

        private class ServiceHubAssetSource : AssetSource
        {
            private readonly JsonRpc _rpc;
            private readonly TraceSource _logger;
            private readonly CancellationToken _cancellationToken;

            public ServiceHubAssetSource(JsonRpc rpc, TraceSource logger, CancellationToken cancellationToken) :
                base()
            {
                _rpc = rpc;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public override async Task RequestAssetAsync(int serviceId, Checksum checksum)
            {
                // it should succeed as long as matching VS is alive
                while (!await TryRequestAssetAsync(serviceId, checksum).ConfigureAwait(false))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                }
            }

            private async Task<bool> TryRequestAssetAsync(int serviceId, Checksum checksum)
            {
                try
                {
                    // TODO: remove stop watch and use logger
                    var stopWatch = Stopwatch.StartNew();

                    await _rpc.InvokeAsync(WellKnownServiceHubServices.AssetService_RequestAsset, new object[] { serviceId, checksum.ToArray() }, (s, c) =>
                    {
                        using (var reader = new ObjectReader(s))
                        {
                            var responseServiceId = reader.ReadInt32();
                            Contract.ThrowIfFalse(serviceId == responseServiceId);

                            var responseChecksum = new Checksum(ImmutableArray.Create(reader.ReadArray<byte>()));
                            Contract.ThrowIfFalse(checksum == responseChecksum);

                            var kind = reader.ReadString();

                            // in service hub, cancellation means simply closed stream
                            var @object = RoslynServices.AssetService.Deserialize<object>(kind, reader, _cancellationToken);

                            _logger.TraceInformation($"({stopWatch.Elapsed}) {kind}");

                            RoslynServices.AssetService.Set(checksum, @object);

                            return SpecializedTasks.EmptyTask;
                        }
                    }, _cancellationToken).ConfigureAwait(false);

                    return true;
                }
                catch (IOException)
                {
                    // this can happen due to many reasons
                    return false;
                }
            }
        }
    }
}
