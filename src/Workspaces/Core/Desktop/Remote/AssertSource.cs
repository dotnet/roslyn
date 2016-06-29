// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class AssetSource
    {
        private static int s_serviceId = 0;
        private readonly int _currentId = 0;

        private readonly JsonRpc _rpc;

        public AssetSource(JsonRpc rpc)
        {
            _rpc = rpc;

            _currentId = Interlocked.Add(ref s_serviceId, 1);

            var manager = RoslynServiceHubServices.Asset;
            manager.RegisterAssetSource(_currentId, this);
        }

        public void RequestAsset(int serviceId, int requestId, Checksum checksum)
        {
            // TODO: change this to use pipe. using this pattern recommended from service hub seems
            //       make things more complicated than needed. using our own pipe seems make things way simpler
            _rpc.InvokeAsync("Request", serviceId, requestId, checksum.ToArray());
        }

        public void Cancelled()
        {
            Done();
        }

        public void Done()
        {
            var manager = RoslynServiceHubServices.Asset;
            manager.UnregisterAssetSource(_currentId);
        }
    }
}
