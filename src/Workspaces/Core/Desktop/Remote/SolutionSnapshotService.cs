// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class SolutionSnapshotService : ServiceHubJsonRpcServiceBase
    {
        private readonly AssetSource _source;

        public SolutionSnapshotService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
            _source = new AssetSource(Rpc);
        }

        public void Done()
        {
            _source.Done();
        }

        protected override void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            _source.Cancelled();
        }
    }
}
