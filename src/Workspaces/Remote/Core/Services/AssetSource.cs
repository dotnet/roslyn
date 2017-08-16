﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected AssetSource(AssetStorage assetStorage)
        {
            _assetStorage = assetStorage;

            _assetStorage.SetAssetSource(this);
        }

        public abstract Task<IList<(Checksum, object)>> RequestAssetsAsync(int scopeId, ISet<Checksum> checksums, CancellationToken cancellationToken);
    }
}
