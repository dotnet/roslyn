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
        private readonly ChecksumTreeCollection _checksumTreeCollections;
        private readonly ChecksumTree _checksumTree;

        public readonly SolutionChecksumObject SolutionChecksum;

        public ChecksumScope(
            ChecksumTreeCollection trees,
            ChecksumTree tree,
            SolutionChecksumObject solutionChecksum)
        {
            _checksumTreeCollections = trees;
            _checksumTree = tree;

            SolutionChecksum = solutionChecksum;

            _checksumTreeCollections.RegisterSnapshot(this, tree);
        }

        public Workspace Workspace => _checksumTree.Solution.Workspace;

        /// <summary>
        /// Add asset that is not part of solution to be part of this snapshot.
        /// 
        /// TODO: currently, this asset must be something <see cref="Serializer"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="ChecksumObject.Kind"/> 
        /// </summary>
        public void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
        {
            _checksumTree.AddAdditionalAsset(asset, cancellationToken);
        }

        public void Dispose()
        {
            _checksumTreeCollections.UnregisterSnapshot(this);
        }
    }
}
