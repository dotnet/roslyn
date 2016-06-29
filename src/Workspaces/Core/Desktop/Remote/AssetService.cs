// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // client will push asset requested to this service.
    // we have this wierd form since service hub doesn't allow service hub to initiate communication
    // to client. it must be alwasy from client to service
    internal class AssetService : ServiceHubRawStreamServiceBase
    {
        private int? _serviceId;
        private int? _requestId;

        public AssetService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        protected override Task WorkerAsync()
        {
            // TODO: refactor this code. for now, make it at least work.
            var manager = RoslynServiceHubServices.Asset;

            using (var reader = new ObjectReader(Stream))
            {
                _serviceId = reader.ReadInt32();
                _requestId = reader.ReadInt32();

                var checksum = new Checksum(ImmutableArray.Create(reader.ReadArray<byte>()));
                var kind = reader.ReadString();

                // in service hub, cancellation means simply closed stream
                var @object = RoslynServiceHubServices.Serializer.Deserialize<object>(kind, reader, CancellationToken.None);
                manager.Set(checksum, @object);
                manager.CloseAssetRequest(_requestId.Value, result: true);
            }

            return SpecializedTasks.EmptyTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (_requestId.HasValue)
            {
                var manager = RoslynServiceHubServices.Asset;
                manager.CloseAssetRequest(_requestId.Value, result: false);
            }
        }
    }
}
