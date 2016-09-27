// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of root checksum tree node
    /// </summary>
    internal partial class ChecksumTreeCollection
    {
        /// <summary>
        /// root tree node of checksum tree
        /// </summary>
        private sealed class RootTreeNode : SubTreeNode, IRootChecksumTreeNode
        {
            // cache to remove lambda allocation
            private readonly static Func<ConcurrentDictionary<Checksum, Asset>> s_additionalAssetsCreator = () => new ConcurrentDictionary<Checksum, Asset>(concurrencyLevel: 2, capacity: 10);

            // cache to remove lambda allocation
            private readonly Func<Checksum, ChecksumObject> _checksumObjectFromAdditionalAssetsGetter;

            // additional assets that is not part of solution but added explicitly
            private ConcurrentDictionary<Checksum, Asset> _additionalAssets;

            public RootTreeNode(ChecksumTreeCollection owner, SolutionState solutionState) :
                base(owner, GetOrCreateSerializer(solutionState.Workspace.Services))
            {
                SolutionState = solutionState;

                _checksumObjectFromAdditionalAssetsGetter = GetChecksumObjectFromAdditionalAssets;
            }

            public SolutionState SolutionState { get; }

            public void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LazyInitialization.EnsureInitialized(ref _additionalAssets, s_additionalAssetsCreator);

                _additionalAssets.TryAdd(asset.Checksum, asset);
            }

            public override ChecksumObject TryGetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                var checksumObject = base.TryGetChecksumObject(checksum, cancellationToken);
                if (checksumObject != null)
                {
                    return checksumObject;
                }

                Asset asset = null;
                if (_additionalAssets?.TryGetValue(checksum, out asset) == true)
                {
                    return asset;
                }

                // this cache has no reference to the given checksum
                return null;
            }

            public override void AppendChecksumObjects(Dictionary<Checksum, ChecksumObject> map, HashSet<Checksum> searchingChecksumsLeft, CancellationToken cancellationToken)
            {
                // call base one to do common search
                base.AppendChecksumObjects(map, searchingChecksumsLeft, cancellationToken);
                if (searchingChecksumsLeft.Count == 0)
                {
                    // there is no checksum left to find
                    return;
                }

                // root tree node has extra data that we need to search as well
                if (_additionalAssets == null)
                {
                    // checksum looks like belong to global asset
                    return;
                }

                AppendChecksumObjectsFromAdditionalAssets(map, searchingChecksumsLeft, cancellationToken);
            }

            private void AppendChecksumObjectsFromAdditionalAssets(
                Dictionary<Checksum, ChecksumObject> map, HashSet<Checksum> searchingChecksumsLeft, CancellationToken cancellationToken)
            {
                AppendChecksumObjects(map, searchingChecksumsLeft, _additionalAssets.Count, _additionalAssets.Keys, _checksumObjectFromAdditionalAssetsGetter, cancellationToken);
            }

            private ChecksumObject GetChecksumObjectFromAdditionalAssets(Checksum checksum)
            {
                Asset asset;
                if (_additionalAssets.TryGetValue(checksum, out asset))
                {
                    return asset;
                }

                // given checksum doesn't exist in this additional assets. but will exist
                // in one of tree nodes/additional assets/global assets
                return null;
            }
        }

        /// <summary>
        /// checksum object with children can have sub checksum tree node.
        /// 
        /// in node, it will either have assets or sub tree nodes for checksum object with children
        /// 
        /// checksum object with children is like node in syntax tree and asset is like token in syntax tree
        /// </summary>
        private class SubTreeNode : IChecksumTreeNode
        {
            // cache to remove lambda allocation
            private static readonly Func<object, ChecksumObjectCache> s_cacheCreator = _ => new ChecksumObjectCache();

            private readonly ChecksumTreeCollection _owner;

            // some of data (checksum) in this cache can be moved into object itself if we decide to do so.
            // this is cache since we can always rebuild these.
            //
            // key is green node such as DoucmentState and value is cache of checksome objects 
            // associated with the green node
            private readonly ConcurrentDictionary<object, ChecksumObjectCache> _cache;

            public SubTreeNode(ChecksumTreeCollection owner, Serializer serializer)
            {
                _owner = owner;
                _cache = new ConcurrentDictionary<object, ChecksumObjectCache>(concurrencyLevel: 2, capacity: 1);

                Serializer = serializer;
            }

            public Serializer Serializer { get; }

            public virtual ChecksumObject TryGetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                ChecksumObject checksumObject;
                foreach (var kv in _cache)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entry = kv.Value;
                    if (entry.TryGetValue(checksum, out checksumObject))
                    {
                        // this cache has information for the checksum
                        return checksumObject;
                    }

                    var cache = entry.TryGetSubTreeNode();
                    if (cache == null)
                    {
                        // this entry doesn't have sub tree cache
                        continue;
                    }

                    // ask its sub tree cache
                    checksumObject = cache.TryGetChecksumObject(checksum, cancellationToken);
                    if (checksumObject != null)
                    {
                        // found one
                        return checksumObject;
                    }
                }

                return null;
            }

            public virtual void AppendChecksumObjects(Dictionary<Checksum, ChecksumObject> map, HashSet<Checksum> searchingChecksumsLeft, CancellationToken cancellationToken)
            {
                if (searchingChecksumsLeft.Count == 0)
                {
                    // there is no checksum left to find
                    return;
                }

                foreach (var kv in _cache)
                {
                    var entry = kv.Value;
                    AppendChecksumObjects(map, searchingChecksumsLeft, entry, cancellationToken);
                    if (searchingChecksumsLeft.Count == 0)
                    {
                        // there is no checksum left to find
                        return;
                    }

                    var cache = entry.TryGetSubTreeNode();
                    if (cache == null)
                    {
                        // this entry doesn't have sub tree cache
                        continue;
                    }

                    // ask its sub tree cache
                    cache.AppendChecksumObjects(map, searchingChecksumsLeft, cancellationToken);
                    if (searchingChecksumsLeft.Count == 0)
                    {
                        // there is no checksum left to find
                        return;
                    }
                }
            }

            public ChecksumObjectCache TryGetChecksumObjectEntry(object key, string kind, CancellationToken cancellationToken)
            {
                // find sub tree node cache that contains given key, kind tuple.
                ChecksumObjectCache self;
                ChecksumObject checksumObject;
                if (_cache.TryGetValue(key, out self) &&
                    self.TryGetValue(kind, out checksumObject))
                {
                    // this cache owns it
                    return self;
                }

                foreach (var kv in _cache)
                {
                    var entry = kv.Value;
                    var tree = entry.TryGetSubTreeNode();
                    if (tree == null)
                    {
                        // this entry doesn't have sub tree cache.
                        continue;
                    }

                    // ask its sub trees cache
                    var subEntry = tree.TryGetChecksumObjectEntry(key, kind, cancellationToken);
                    if (subEntry != null)
                    {
                        // found one
                        return subEntry;
                    }
                }

                // this cache has no reference to the given checksum
                return null;
            }

            public IChecksumTreeNode GetOrCreateSubTreeNode<TKey>(TKey key)
            {
                var entry = _cache.GetOrAdd(key, s_cacheCreator);
                return entry.GetOrCreateSubTreeNode(_owner, Serializer);
            }

            public async Task<TChecksumObject> GetOrCreateChecksumObjectWithChildrenAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TKey, TValue, string, CancellationToken, Task<TChecksumObject>> valueGetterAsync, CancellationToken cancellationToken)
                where TKey : class
                where TChecksumObject : ChecksumObjectWithChildren
            {
                return await GetOrCreateChecksumObjectAsync(key, value, kind, (v, k, c) => valueGetterAsync(key, v, k, c), cancellationToken).ConfigureAwait(false);
            }

            public TChecksumObject GetOrCreateChecksumObjectWithChildren<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TKey, TValue, string, CancellationToken, TChecksumObject> valueGetter, CancellationToken cancellationToken)
                where TKey : class
                where TChecksumObject : ChecksumObjectWithChildren
            {
                return GetOrCreateChecksumObject(key, value, kind, (v, k, c) => valueGetter(key, v, k, c), cancellationToken);
            }

            public Asset GetOrCreateAsset<TKey, TValue>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Asset> valueGetter, CancellationToken cancellationToken)
                where TKey : class
            {
                return GetOrCreateChecksumObject(key, value, kind, valueGetter, cancellationToken);
            }

            protected static void AppendChecksumObjects(
                Dictionary<Checksum, ChecksumObject> map,
                HashSet<Checksum> searchingChecksumsLeft,
                int currentNodeChecksumCount,
                IEnumerable<Checksum> currentNodeChecksums,
                Func<Checksum, ChecksumObject> checksumGetterForCurrentNode,
                CancellationToken cancellationToken)
            {
                // this will iterate through candidate checksums to see whether that checksum exists in both
                // checksum set we are currently searching for and checksums current node contains
                using (var removed = Creator.CreateList<Checksum>())
                {
                    // we have 2 sets of checksums. one we are searching for and ones this node contains.
                    // we only need to iterate one of them to see this node contains what we are looking for.
                    // so, we check two set and use one that has smaller number of checksums.
                    foreach (var checksum in GetSmallerChecksumList(searchingChecksumsLeft.Count, searchingChecksumsLeft, currentNodeChecksumCount, currentNodeChecksums))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // if checksumGetter return null for given checksum, that means current node doesn't have given checksum
                        var checksumObject = checksumGetterForCurrentNode(checksum);
                        if (checksumObject != null && searchingChecksumsLeft.Contains(checksum))
                        {
                            // found given checksum in current node
                            map[checksum] = checksumObject;
                            removed.Object.Add(checksum);

                            // we found all checksums we are looking for
                            if (removed.Object.Count == searchingChecksumsLeft.Count)
                            {
                                break;
                            }
                        }
                    }

                    searchingChecksumsLeft.ExceptWith(removed.Object);
                }
            }

            private static IEnumerable<Checksum> GetSmallerChecksumList(int count1, IEnumerable<Checksum> checksums1, int count2, IEnumerable<Checksum> checksums2)
            {
                // return smaller checksum list from given two list
                return count1 < count2 ? checksums1 : checksums2;
            }

            private void AppendChecksumObjects(
                Dictionary<Checksum, ChecksumObject> map, HashSet<Checksum> searchingChecksumsLeft, ChecksumObjectCache cache, CancellationToken cancellationToken)
            {
                AppendChecksumObjects(map, searchingChecksumsLeft, cache.Count, cache.GetChecksums(), c => GetChecksumObjectFromTreeNode(cache, c), cancellationToken);
            }

            private ChecksumObject GetChecksumObjectFromTreeNode(ChecksumObjectCache cache, Checksum checksum)
            {
                ChecksumObject checksumObject;
                if (cache.TryGetValue(checksum, out checksumObject))
                {
                    return checksumObject;
                }

                // given checksum doesn't exist in this entry of tree node. but will exist
                // in one of tree nodes/additional assets/global assets
                return null;
            }

            private async Task<TChecksumObject> GetOrCreateChecksumObjectAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TChecksumObject>> valueGetterAsync, CancellationToken cancellationToken)
                where TKey : class where TChecksumObject : ChecksumObject
            {
                using (Logger.LogBlock(FunctionId.ChecksumTreeNode_GetOrCreateChecksumObjectAsync, CreateLogMessage, key, kind, cancellationToken))
                {
                    Contract.ThrowIfNull(key);

                    // ask myself
                    ChecksumObject checksumObject;
                    var entry = TryGetChecksumObjectEntry(key, kind, cancellationToken);
                    if (entry != null && entry.TryGetValue(kind, out checksumObject))
                    {
                        return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
                    }

                    // ask owner
                    entry = _owner.TryGetChecksumObjectEntry(key, kind, cancellationToken);
                    if (entry == null)
                    {
                        // owner doesn't have it, create one.
                        checksumObject = await valueGetterAsync(value, kind, cancellationToken).ConfigureAwait(false);
                    }
                    else if (!entry.TryGetValue(kind, out checksumObject))
                    {
                        // owner doesn't have this particular kind, create one.
                        checksumObject = await valueGetterAsync(value, kind, cancellationToken).ConfigureAwait(false);
                    }

                    // record local copy (reference) and return it.
                    // REVIEW: we can go ref count route rather than this (local copy). but then we need to make sure there is no leak.
                    //         for now, we go local copy route since overhead is small (just duplicated reference pointer), but reduce complexity a lot.
                    return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
                }
            }

            private TChecksumObject GetOrCreateChecksumObject<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, TChecksumObject> valueGetter, CancellationToken cancellationToken)
                where TKey : class where TChecksumObject : ChecksumObject
            {
                using (Logger.LogBlock(FunctionId.ChecksumTreeNode_GetOrCreateChecksumObject, CreateLogMessage, key, kind, cancellationToken))
                {
                    Contract.ThrowIfNull(key);

                    // ask myself
                    ChecksumObject checksumObject;
                    var entry = TryGetChecksumObjectEntry(key, kind, cancellationToken);
                    if (entry != null && entry.TryGetValue(kind, out checksumObject))
                    {
                        return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
                    }

                    // ask owner
                    entry = _owner.TryGetChecksumObjectEntry(key, kind, cancellationToken);
                    if (entry == null)
                    {
                        // owner doesn't have it, create one.
                        checksumObject = valueGetter(value, kind, cancellationToken);
                    }
                    else if (!entry.TryGetValue(kind, out checksumObject))
                    {
                        // owner doesn't have this particular kind, create one.
                        checksumObject = valueGetter(value, kind, cancellationToken);
                    }

                    // record local copy (reference) and return it.
                    // REVIEW: we can go ref count route rather than this (local copy). but then we need to make sure there is no leak.
                    //         for now, we go local copy route since overhead is small (just duplicated reference pointer), but reduce complexity a lot.
                    return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
                }
            }

            private ChecksumObject SaveAndReturn(object key, ChecksumObject checksumObject, ChecksumObjectCache entry = null)
            {
                // create new entry if it is not already given
                ChecksumObjectCache self;
                if (_cache.TryGetValue(key, out self))
                {
                    // we already have entry
                    return self.Add(checksumObject);
                }

                // either create new entry or re-use given one. this will let us share
                // whole sub checksum tree that is associated with given green node (key)
                entry = _cache.GetOrAdd(key, _ => entry ?? new ChecksumObjectCache());
                return entry.Add(checksumObject);
            }

            private static string CreateLogMessage<T>(T key, string kind)
            {
                return $"{kind} - {GetLogInfo(key) ?? "unknown"}";
            }

            private static string GetLogInfo<T>(T key)
            {
                var solutionState = key as SolutionState;
                if (solutionState != null)
                {
                    return solutionState.FilePath;
                }

                var projectState = key as ProjectState;
                if (projectState != null)
                {
                    return projectState.FilePath;
                }

                var documentState = key as DocumentState;
                if (documentState != null)
                {
                    return documentState.FilePath;
                }

                return "no detail";
            }
        }
    }
}
