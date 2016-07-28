// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// checksum scope that one can use to pin assets in memory while working on remote host
    /// </summary>
    internal class ChecksumScope : IDisposable
    {
        private readonly ChecksumTreeNodeCacheCollection _caches;
        private readonly ChecksumTreeNodeCache _cache;

        public readonly SolutionChecksumObject SolutionChecksum;

        public ChecksumScope(
            ChecksumTreeNodeCacheCollection caches,
            ChecksumTreeNodeCache cache,
            SolutionChecksumObject solutionChecksum)
        {
            _caches = caches;
            _cache = cache;

            SolutionChecksum = solutionChecksum;

            _caches.RegisterSnapshot(this, cache);
        }

        public Workspace Workspace => _cache.Solution.Workspace;

        /// <summary>
        /// Add asset that is not part of solution to be part of this snapshot.
        /// 
        /// TODO: currently, this asset must be something <see cref="Serializer"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="ChecksumObject.Kind"/> 
        /// </summary>
        public void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
        {
            _cache.AddAdditionalAsset(asset, cancellationToken);
        }

        public void Dispose()
        {
            _caches.UnregisterSnapshot(this);
        }
    }
}
