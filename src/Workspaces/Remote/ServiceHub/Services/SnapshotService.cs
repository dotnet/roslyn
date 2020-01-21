// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Snapshot service in service hub side.
    /// 
    /// this service will be used to move over remotable data from client to service hub
    /// </summary>
    internal partial class SnapshotService : ServiceHubServiceBase
    {
        private readonly AssetSource _source;

        public SnapshotService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _source = new JsonRpcAssetSource(this);

            StartService();
        }
    }
}
