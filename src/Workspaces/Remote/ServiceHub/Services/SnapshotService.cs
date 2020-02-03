// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
