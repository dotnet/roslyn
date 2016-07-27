// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Asset source provides a way to callback asset source (Ex, VS) to get asset with the given checksum
    /// </summary>
    internal abstract class AssetSource
    {
        private static int s_serviceId = 0;
        private readonly int _currentId = 0;

        protected AssetSource()
        {
            _currentId = Interlocked.Add(ref s_serviceId, 1);

            RoslynServices.AssetService.RegisterAssetSource(_currentId, this);
        }

        public abstract Task<object> RequestAssetAsync(int serviceId, Checksum checksum, CancellationToken cancellationToken);

        public void Done()
        {
            RoslynServices.AssetService.UnregisterAssetSource(_currentId);
        }
    }
}
