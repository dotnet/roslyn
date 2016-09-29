// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Snapshot service in service hub side.
    /// 
    /// this service will be used to move over snapshot data from client to service hub
    /// </summary>
    internal partial class SnapshotService : ServiceHubServiceBase
    {
        private readonly AssetSource _source;

        public SnapshotService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
            _source = new JsonRpcAssetSource(Rpc, Logger, CancellationToken);
        }

        protected override void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            _source.Done();
        }
    }
}