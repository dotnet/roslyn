// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Asset source provides a way to callback asset source (Ex, VS) to get asset with the given checksum
    /// </summary>
    internal abstract class AssetSource
    {
        private readonly AssetStorage _assetStorage;
        private readonly int _scopeId;

        protected AssetSource(AssetStorage assetStorage, int scopeId)
        {
            _assetStorage = assetStorage;
            _scopeId = scopeId;

            _assetStorage.RegisterAssetSource(_scopeId, this);
        }

        public abstract Task<IList<ValueTuple<Checksum, object>>> RequestAssetsAsync(int scopeId, ISet<Checksum> checksums, CancellationToken cancellationToken);

        public void Done()
        {
            _assetStorage.UnregisterAssetSource(_scopeId);
        }
    }
}
