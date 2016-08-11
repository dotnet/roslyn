// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of checksum root tree node
    /// </summary>
    internal partial class ChecksumTreeCollection
    {
        /// <summary>
        /// global asset is an asset which life time is same as host
        /// </summary>
        private readonly ConcurrentDictionary<object, Asset> _globalAssets;

        /// <summary>
        /// map from solution checksum scope to its root checksum tree node
        /// </summary>
        private readonly ConcurrentDictionary<ChecksumScope, RootTreeNode> _rootTreeNodes;

        public ChecksumTreeCollection()
        {
            _globalAssets = new ConcurrentDictionary<object, Asset>(concurrencyLevel: 2, capacity: 10);
            _rootTreeNodes = new ConcurrentDictionary<ChecksumScope, RootTreeNode>(concurrencyLevel: 2, capacity: 10);

            // TODO: currently only red node we are holding in this cache is Solution. create SolutionState so
            //       that we don't hold onto any red nodes (such as Document/Project)
        }

        public void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_globalAssets.TryAdd(value, asset))
            {
                // there is existing one, make sure asset is same
                Contract.ThrowIfFalse(_globalAssets[value].Checksum == asset.Checksum);
            }
        }

        public Asset GetGlobalAsset(object value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Asset asset;
            _globalAssets.TryGetValue(value, out asset);

            return asset;
        }

        public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Asset asset;
            _globalAssets.TryRemove(value, out asset);
        }

        public IRootChecksumTreeNode CreateRootTreeNode(Solution solution)
        {
            return new RootTreeNode(this, solution);
        }

        public ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
        {
            // search snapshots we have
            foreach (var cache in _rootTreeNodes.Values)
            {
                var checksumObject = cache.TryGetChecksumObject(checksum, cancellationToken);
                if (checksumObject != null)
                {
                    return checksumObject;
                }
            }

            // search global assets
            foreach (var asset in _globalAssets.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (asset.Checksum == checksum)
                {
                    return asset;
                }
            }

            // as long as solution snapshot is pinned. it must exist in one of the trees.
            throw ExceptionUtilities.UnexpectedValue(checksum);
        }

        private ChecksumObjectCache TryGetChecksumObjectEntry(object key, string kind, CancellationToken cancellationToken)
        {
            foreach (var cache in _rootTreeNodes.Values)
            {
                var entry = cache.TryGetChecksumObjectEntry(key, kind, cancellationToken);
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        public void RegisterSnapshot(ChecksumScope snapshot, IRootChecksumTreeNode cache)
        {
            // duplicates are not allowed, there can be multiple snapshots to same solution, so no ref counting.
            Contract.ThrowIfFalse(_rootTreeNodes.TryAdd(snapshot, (RootTreeNode)cache));
        }

        public void UnregisterSnapshot(ChecksumScope snapshot)
        {
            // calling it multiple times for same snapshot is not allowed.
            RootTreeNode dummy;
            Contract.ThrowIfFalse(_rootTreeNodes.TryRemove(snapshot, out dummy));
        }
    }
}
