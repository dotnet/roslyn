// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of checksum root tree node
    /// </summary>
    internal partial class ChecksumTreeCollection
    {
        // serializer and empty checksum collection task cache - this is to reduce allocations
        private readonly static ConditionalWeakTable<HostWorkspaceServices, Serializer> s_serializerCache = new ConditionalWeakTable<HostWorkspaceServices, Serializer>();
        private readonly static ConditionalWeakTable<Serializer, ConcurrentDictionary<string, ChecksumCollection>> s_emptyChecksumCollectionCache = new ConditionalWeakTable<Serializer, ConcurrentDictionary<string, ChecksumCollection>>();
        private readonly static ConditionalWeakTable<ChecksumCollection, Task<ChecksumCollection>> s_emptyChecksumCollectionTaskCache = new ConditionalWeakTable<ChecksumCollection, Task<ChecksumCollection>>();

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

        public IRootChecksumTreeNode CreateRootTreeNode(SolutionState solutionState)
        {
            return new RootTreeNode(this, solutionState);
        }

        public ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return Asset.Null;
            }

            // search snapshots we have
            foreach (var kv in _rootTreeNodes)
            {
                var cache = kv.Value;
                var checksumObject = cache.TryGetChecksumObject(checksum, cancellationToken);
                if (checksumObject != null)
                {
                    return checksumObject;
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

            // as long as solution snapshot is pinned. it must exist in one of the trees.
            throw ExceptionUtilities.UnexpectedValue(checksum);
        }

        public IReadOnlyDictionary<Checksum, ChecksumObject> GetChecksumObjects(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (var searchingChecksumsLeft = Creator.CreateChecksumSet(checksums))
            {
                var numberOfChecksumsToSearch = searchingChecksumsLeft.Object.Count;
                var result = new Dictionary<Checksum, ChecksumObject>(numberOfChecksumsToSearch);

                // check nil case
                if (searchingChecksumsLeft.Object.Remove(Checksum.Null))
                {
                    result[Checksum.Null] = Asset.Null;
                }

                // search checksum trees we have
                foreach (var kv in _rootTreeNodes)
                {
                    var cache = kv.Value;
                    cache.AppendChecksumObjects(result, searchingChecksumsLeft.Object, cancellationToken);
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

                // as long as solution snapshot is pinned. it must exist in one of the trees.
                throw ExceptionUtilities.UnexpectedValue(result.Count);
            }
        }

        private ChecksumObjectCache TryGetChecksumObjectEntry(object key, string kind, CancellationToken cancellationToken)
        {
            foreach (var kv in _rootTreeNodes)
            {
                var cache = kv.Value;
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

        private static readonly ConditionalWeakTable<HostWorkspaceServices, Serializer>.CreateValueCallback s_serializerCallback = s => new Serializer(s);

        public static Serializer GetOrCreateSerializer(HostWorkspaceServices services)
        {
            return s_serializerCache.GetValue(services, s_serializerCallback);
        }

        private static readonly ConditionalWeakTable<Serializer, ConcurrentDictionary<string, ChecksumCollection>>.CreateValueCallback s_emptyChecksumCollectionCallback =
            s => new ConcurrentDictionary<string, ChecksumCollection>(concurrencyLevel: 2, capacity: 20);

        public static ChecksumCollection GetOrCreateEmptyChecksumCollection(Serializer serializer, string kind)
        {
            var map = s_emptyChecksumCollectionCache.GetValue(serializer, s_emptyChecksumCollectionCallback);

            ChecksumCollection collection;
            if (map.TryGetValue(kind, out collection))
            {
                return collection;
            }

            return map.GetOrAdd(kind, _ => new ChecksumCollection(serializer, kind, Array.Empty<object>()));
        }

        private static readonly ConditionalWeakTable<ChecksumCollection, Task<ChecksumCollection>>.CreateValueCallback s_emptyChecksumCollectionTaskCallback = c => Task.FromResult(c);

        public static Task<ChecksumCollection> GetOrCreateEmptyChecksumCollectionTask(Serializer serializer, string kind)
        {
            var collection = GetOrCreateEmptyChecksumCollection(serializer, kind);
            return s_emptyChecksumCollectionTaskCache.GetValue(collection, s_emptyChecksumCollectionTaskCallback);
        }
    }
}
