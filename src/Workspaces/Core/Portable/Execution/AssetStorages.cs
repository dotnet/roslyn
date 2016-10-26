﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of asset storage
    /// </summary>
    internal partial class AssetStorages
    {
        /// <summary>
        /// global asset is an asset which life time is same as host
        /// </summary>
        private readonly ConcurrentDictionary<object, CustomAsset> _globalAssets;

        /// <summary>
        /// map from solution checksum scope to its associated asset storage
        /// </summary>
        private readonly ConcurrentDictionary<PinnedRemotableDataScope, Storage> _storages;

        public AssetStorages()
        {
            _globalAssets = new ConcurrentDictionary<object, CustomAsset>(concurrencyLevel: 2, capacity: 10);
            _storages = new ConcurrentDictionary<PinnedRemotableDataScope, Storage>(concurrencyLevel: 2, capacity: 10);
        }

        public void AddGlobalAsset(object value, CustomAsset asset, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_globalAssets.TryAdd(value, asset))
            {
                // there is existing one, make sure asset is same
                Contract.ThrowIfFalse(_globalAssets[value].Checksum == asset.Checksum);
            }
        }

        public CustomAsset GetGlobalAsset(object value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CustomAsset asset;
            _globalAssets.TryGetValue(value, out asset);

            return asset;
        }

        public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CustomAsset asset;
            _globalAssets.TryRemove(value, out asset);
        }

        public Storage CreateStorage(SolutionState solutionState)
        {
            return new Storage(this, solutionState);
        }

        public RemotableData GetRemotableData(PinnedRemotableDataScope scope, Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return RemotableData.Null;
            }

            // search snapshots we have
            foreach (var storage in GetStorages(scope))
            {
                var syncObject = storage.TryGetRemotableData(checksum, cancellationToken);
                if (syncObject != null)
                {
                    return syncObject;
                }
            }

            // search global assets
            foreach (var kv in _globalAssets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var asset = kv.Value;
                if (asset.Checksum == checksum)
                {
                    return asset;
                }
            }

            // if it reached here, it means things get cancelled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things cancelled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return null;
        }

        public IReadOnlyDictionary<Checksum, RemotableData> GetRemotableData(PinnedRemotableDataScope scope, IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (var searchingChecksumsLeft = Creator.CreateChecksumSet(checksums))
            {
                var numberOfChecksumsToSearch = searchingChecksumsLeft.Object.Count;
                var result = new Dictionary<Checksum, RemotableData>(numberOfChecksumsToSearch);

                // check nil case
                if (searchingChecksumsLeft.Object.Remove(Checksum.Null))
                {
                    result[Checksum.Null] = RemotableData.Null;
                }

                // search checksum trees we have
                foreach (var storage in GetStorages(scope))
                {
                    storage.AppendRemotableData(searchingChecksumsLeft.Object, result, cancellationToken);
                    if (result.Count == numberOfChecksumsToSearch)
                    {
                        // no checksum left to find
                        Contract.Requires(searchingChecksumsLeft.Object.Count == 0);
                        return result;
                    }
                }

                // search global assets
                foreach (var kv in _globalAssets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var asset = kv.Value;
                    if (searchingChecksumsLeft.Object.Remove(asset.Checksum))
                    {
                        result[asset.Checksum] = asset;

                        if (result.Count == numberOfChecksumsToSearch)
                        {
                            // no checksum left to find
                            Contract.Requires(searchingChecksumsLeft.Object.Count == 0);
                            return result;
                        }
                    }
                }

                // if it reached here, it means things get cancelled. due to involving 2 processes,
                // current design can make slightly staled requests to running even when things cancelled.
                // if it is other case, remote host side will throw and close connection which will cause
                // vs to crash.
                // this should be changed once I address this design issue
                cancellationToken.ThrowIfCancellationRequested();

                return result;
            }
        }

        public void RegisterSnapshot(PinnedRemotableDataScope snapshot, AssetStorages.Storage storage)
        {
            // duplicates are not allowed, there can be multiple snapshots to same solution, so no ref counting.
            Contract.ThrowIfFalse(_storages.TryAdd(snapshot, storage));
        }

        public void UnregisterSnapshot(PinnedRemotableDataScope snapshot)
        {
            // calling it multiple times for same snapshot is not allowed.
            Storage dummy;
            Contract.ThrowIfFalse(_storages.TryRemove(snapshot, out dummy));
        }

        private IEnumerable<Storage> GetStorages(PinnedRemotableDataScope scope)
        {
            if (scope != null)
            {
                yield return _storages[scope];
                yield break;
            }

            using (var solutionProcessed = Creator.CreateChecksumSet())
            {
                foreach (var kv in _storages)
                {
                    if (!solutionProcessed.Object.Add(kv.Key.SolutionChecksum))
                    {
                        continue;
                    }

                    yield return kv.Value;
                }
            }
        }
    }
}
