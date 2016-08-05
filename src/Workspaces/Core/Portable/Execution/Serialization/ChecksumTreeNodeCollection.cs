// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of checksum tree node cache
    /// </summary>
    internal class ChecksumTreeNodeCacheCollection
    {
        /// <summary>
        /// global asset is an asset which life time is same as host
        /// </summary>
        private readonly ConcurrentDictionary<object, Asset> _globalAssets;

        /// <summary>
        /// map from solution checksum scope to its root of checksum treenode cache
        /// </summary>
        private readonly ConcurrentDictionary<ChecksumScope, Cache> _treeNodeCaches;

        public ChecksumTreeNodeCacheCollection()
        {
            _globalAssets = new ConcurrentDictionary<object, Asset>(concurrencyLevel: 2, capacity: 10);
            _treeNodeCaches = new ConcurrentDictionary<ChecksumScope, Cache>(concurrencyLevel: 2, capacity: 10);

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

        public ChecksumTreeNodeCache CreateRootTreeNodeCache(Solution solution)
        {
            return new Cache(this, solution);
        }

        public ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
        {
            // search snapshots we have
            foreach (var cache in _treeNodeCaches.Values)
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
            foreach (var cache in _treeNodeCaches.Values)
            {
                var entry = cache.TryGetChecksumObjectEntry(key, kind, cancellationToken);
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        public void RegisterSnapshot(ChecksumScope snapshot, ChecksumTreeNodeCache cache)
        {
            // duplicates are not allowed, there can be multiple snapshots to same solution, so no ref counting.
            Contract.ThrowIfFalse(_treeNodeCaches.TryAdd(snapshot, (Cache)cache));
        }

        public void UnregisterSnapshot(ChecksumScope snapshot)
        {
            // calling it multiple times for same snapshot is not allowed.
            Cache dummy;
            Contract.ThrowIfFalse(_treeNodeCaches.TryRemove(snapshot, out dummy));
        }

        /// <summary>
        /// Each checksum object with children has 1 corresponding checksum treenode cache.
        /// 
        /// in cahce, it will either have assets or sub tree caches for checksum object with children
        /// 
        /// think this as a node in syntax tree, asset as token in the tree.
        /// </summary>
        private sealed class Cache : ChecksumTreeNodeCache
        {
            private readonly ChecksumTreeNodeCacheCollection _owner;

            // some of data (checksum) in this cache can be moved into object itself if we decide to do so.
            // this is cache since we can always rebuild these.
            //
            // key is green node such as DoucmentState and value is cache of checksome objects 
            // associated with the green node
            private readonly ConcurrentDictionary<object, ChecksumObjectCache> _cache;

            // additional assets that is not part of solution but added explicitly
            private ConcurrentDictionary<Checksum, Asset> _additionalAssets;

            public Cache(ChecksumTreeNodeCacheCollection owner, Solution solution) :
                base(solution)
            {
                _owner = owner;
                _cache = new ConcurrentDictionary<object, ChecksumObjectCache>(concurrencyLevel: 2, capacity: 1);

                // TODO: specialize root checksum tree node cache vs all sub tree node cache
            }

            public override void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LazyInitialization.EnsureInitialized(ref _additionalAssets, () => new ConcurrentDictionary<Checksum, Asset>());

                _additionalAssets.TryAdd(asset.Checksum, asset);
            }

            public ChecksumObject TryGetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                // search needed to be improved.
                ChecksumObject checksumObject;
                foreach (var entry in _cache.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (entry.TryGetValue(checksum, out checksumObject))
                    {
                        // this cache has information for the checksum
                        return checksumObject;
                    }

                    var cache = entry.TryGetSubTreeNodeCache();
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

                Asset asset = null;
                if (_additionalAssets?.TryGetValue(checksum, out asset) == true)
                {
                    return asset;
                }

                // this cache has no reference to the given checksum
                return null;
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

                foreach (var entry in _cache.Values)
                {
                    var tree = entry.TryGetSubTreeNodeCache();
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

            public override async Task<TChecksumObject> GetOrCreateChecksumObjectWithChildrenAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, SnapshotBuilder, AssetBuilder, CancellationToken, Task<TChecksumObject>> valueGetterAsync, CancellationToken cancellationToken)
            {
                return await GetOrCreateChecksumObjectAsync(key, value, kind, (v, k, c) =>
                {
                    var snapshotBuilder = new SnapshotBuilder(GetOrCreateSubTreeNodeCache(key));
                    var assetBuilder = new AssetBuilder(this);

                    return valueGetterAsync(v, k, snapshotBuilder, assetBuilder, c);
                }, cancellationToken).ConfigureAwait(false);
            }

            public override Task<TAsset> GetOrCreateAssetAsync<TKey, TValue, TAsset>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TAsset>> valueGetterAsync, CancellationToken cancellationToken)
            {
                return GetOrCreateChecksumObjectAsync(key, value, kind, valueGetterAsync, cancellationToken);
            }

            private async Task<TChecksumObject> GetOrCreateChecksumObjectAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TChecksumObject>> valueGetterAsync, CancellationToken cancellationToken)
                where TKey : class where TChecksumObject : ChecksumObject
            {
                using (Logger.LogBlock(FunctionId.ChecksumTreeNodeCache_GetOrCreateChecksumObjectAsync, CreateLogMessage, key, kind, cancellationToken))
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

            private ChecksumObject SaveAndReturn(object key, ChecksumObject checksumObject, ChecksumObjectCache entry = null)
            {
                // create new entry if it is not already given
                entry = _cache.GetOrAdd(key, _ => entry ?? new ChecksumObjectCache(checksumObject));
                return entry.Add(checksumObject);
            }

            private ChecksumTreeNodeCache GetOrCreateSubTreeNodeCache<TKey>(TKey key)
            {
                var entry = _cache.GetOrAdd(key, _ => new ChecksumObjectCache());
                return entry.GetOrCreateSubTreeNodeCache(_owner, Solution);
            }

            private static string CreateLogMessage<T>(T key, string kind)
            {
                return $"{kind} - {GetLogInfo(key) ?? "unknown"}";
            }

            private static string GetLogInfo<T>(T key)
            {
                var solution = key as Solution;
                if (solution != null)
                {
                    return solution.FilePath;
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

        /// <summary>
        /// kind to actual checksum object collection
        /// 
        /// since our hierarchical checksum tree should only hold onto green node. 
        /// different kind of checksum object might share same green node such as document state
        /// for SourceText and DocumentInfo. so we have this nested cache type
        /// </summary>
        private class ChecksumObjectCache
        {
            private ChecksumObject _checksumObject;

            private Cache _lazyChecksumTree;
            private ConcurrentDictionary<string, ChecksumObject> _lazyKindToChecksumObjectMap;
            private ConcurrentDictionary<Checksum, ChecksumObject> _lazyChecksumToChecksumObjectMap;

            public ChecksumObjectCache()
            {
            }

            public ChecksumObjectCache(ChecksumObject checksumObject)
            {
                _checksumObject = checksumObject;
            }

            public ChecksumObject Add(ChecksumObject checksumObject)
            {
                Interlocked.CompareExchange(ref _checksumObject, checksumObject, null);

                // optimization to not create map. in whole solution level, there is
                // many case where green node is unique per checksum object such as metadata reference
                // or p2p reference.
                if (_checksumObject.Kind == checksumObject.Kind)
                {
                    // we already have one
                    Contract.Requires(_checksumObject.Checksum.Equals(checksumObject.Checksum));
                    return _checksumObject;
                }

                // more expansive case. create map to save checksum object per kind
                EnsureLazyMap();

                _lazyChecksumToChecksumObjectMap.TryAdd(checksumObject.Checksum, checksumObject);

                if (_lazyKindToChecksumObjectMap.TryAdd(checksumObject.Kind, checksumObject))
                {
                    // just added new one
                    return checksumObject;
                }

                // there is existing one.
                return _lazyKindToChecksumObjectMap[checksumObject.Kind];
            }

            public bool TryGetValue(string kind, out ChecksumObject checksumObject)
            {
                if (_checksumObject?.Kind == kind)
                {
                    checksumObject = _checksumObject;
                    return true;
                }

                if (_lazyKindToChecksumObjectMap != null)
                {
                    return _lazyKindToChecksumObjectMap.TryGetValue(kind, out checksumObject);
                }

                checksumObject = null;
                return false;
            }

            public bool TryGetValue(Checksum checksum, out ChecksumObject checksumObject)
            {
                if (_checksumObject?.Checksum == checksum)
                {
                    checksumObject = _checksumObject;
                    return true;
                }

                if (_lazyChecksumToChecksumObjectMap != null)
                {
                    return _lazyChecksumToChecksumObjectMap.TryGetValue(checksum, out checksumObject);
                }

                checksumObject = null;
                return false;
            }

            public Cache TryGetSubTreeNodeCache()
            {
                return _lazyChecksumTree;
            }

            public Cache GetOrCreateSubTreeNodeCache(ChecksumTreeNodeCacheCollection owner, Solution solution)
            {
                if (_lazyChecksumTree != null)
                {
                    return _lazyChecksumTree;
                }

                Interlocked.CompareExchange(ref _lazyChecksumTree, new Cache(owner, solution), null);
                return _lazyChecksumTree;
            }

            private void EnsureLazyMap()
            {
                if (_lazyKindToChecksumObjectMap == null)
                {
                    // we have multiple entries. create lazy map
                    Interlocked.CompareExchange(ref _lazyKindToChecksumObjectMap, new ConcurrentDictionary<string, ChecksumObject>(concurrencyLevel: 2, capacity: 1), null);
                }

                if (_lazyChecksumToChecksumObjectMap == null)
                {
                    // we have multiple entries. create lazy map
                    Interlocked.CompareExchange(ref _lazyChecksumToChecksumObjectMap, new ConcurrentDictionary<Checksum, ChecksumObject>(concurrencyLevel: 2, capacity: 1), null);
                }
            }
        }
    }

    /// <summary>
    /// Base type for checksum tree node cache. we currently have 2 implementation.
    /// 
    /// one that used for hierarchical tree, one that is used to create one off asset
    /// from asset builder such as additional or global asset which is not part of
    /// hierarchical checksum tree
    /// </summary>
    internal abstract class ChecksumTreeNodeCache
    {
        public readonly Solution Solution;
        public readonly Serializer Serializer;

        protected ChecksumTreeNodeCache(Solution solution)
        {
            Solution = solution;
            Serializer = new Serializer(solution.Workspace.Services);
        }

        public abstract void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken);

        // TResult since Task doesn't allow covariant
        public abstract Task<TResult> GetOrCreateChecksumObjectWithChildrenAsync<TKey, TValue, TResult>(
            TKey key, TValue value, string kind,
            Func<TValue, string, SnapshotBuilder, AssetBuilder, CancellationToken, Task<TResult>> valueGetterAsync,
            CancellationToken cancellationToken)
            where TKey : class
            where TResult : ChecksumObjectWithChildren;

        // TResult since Task doesn't allow covariant
        public abstract Task<TResult> GetOrCreateAssetAsync<TKey, TValue, TResult>(
            TKey key, TValue value, string kind,
            Func<TValue, string, CancellationToken, Task<TResult>> valueGetterAsync,
            CancellationToken cancellationToken)
            where TKey : class
            where TResult : Asset;
    }
}
