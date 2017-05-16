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
        // use gate to make sure same value is seen by multiple threads correctly.
        // initialize and disconnect can be called concurrently due to the way
        // we implements cancellation
        private readonly object _gate;
        private AssetSource _source;

        public SnapshotService(Stream stream, IServiceProvider serviceProvider) :
            base(serviceProvider, stream)
        {
            _gate = new object();

            Rpc.StartListening();
        }

        public override void Initialize(int sessionId, bool primary, Checksum solutionChecksum)
        {
            base.Initialize(sessionId, primary, solutionChecksum);

            lock (_gate)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _source = new JsonRpcAssetSource(this, sessionId);
            }
        }

        protected override void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            lock (_gate)
            {
                // operation can be cancelled even before initialize is called. 
                // or in the middle of initialize is running
                _source?.Done();
            }
        }
    }
}