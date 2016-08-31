// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            // additional assets that is not part of solution but added explicitly
            private ConcurrentDictionary<Checksum, Asset> _additionalAssets;

            public RootTreeNode(ChecksumTreeCollection owner, SolutionState solutionState) :
                base(owner, GetOrCreateSerializer(solutionState.Workspace.Services))
            {
                SolutionState = solutionState;
            }

            public SolutionState SolutionState { get; }

            public void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LazyInitialization.EnsureInitialized(ref _additionalAssets, () => new ConcurrentDictionary<Checksum, Asset>(concurrencyLevel: 2, capacity: 10));

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

            public override void AppendChecksumObjects(ImmutableDictionary<Checksum, ChecksumObject>.Builder builder, HashSet<Checksum> checksums, CancellationToken cancellationToken)
            {
                base.AppendChecksumObjects(builder, checksums, cancellationToken);
                if (checksums.Count == 0)
                {
                    return;
                }

                if (_additionalAssets == null)
                {
                    // this can't be reached
                    throw ExceptionUtilities.Unreachable;
                }

                AppendChecksumObjects(builder, checksums, _additionalAssets, cancellationToken);
            }

            private void AppendChecksumObjects(
                ImmutableDictionary<Checksum, ChecksumObject>.Builder builder, HashSet<Checksum> checksums, ConcurrentDictionary<Checksum, Asset> assets, CancellationToken cancellationToken)
            {
                AppendChecksumObjects(builder, checksums, assets.Count, assets.Keys, c => GetItem(assets, c), cancellationToken);
            }

            private ChecksumObject GetItem(ConcurrentDictionary<Checksum, Asset> assets, Checksum checksum)
            {
                Asset asset;
                if (assets.TryGetValue(checksum, out asset))
                {
                    return asset;
                }

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
                foreach (var entry in _cache.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

            public virtual void AppendChecksumObjects(ImmutableDictionary<Checksum, ChecksumObject>.Builder builder, HashSet<Checksum> checksums, CancellationToken cancellationToken)
            {
                if (checksums.Count == 0)
                {
                    return;
                }

                foreach (var entry in _cache.Values)
                {
                    AppendChecksumObjects(builder, checksums, entry, cancellationToken);
                    if (checksums.Count == 0)
                    {
                        // we found all
                        return;
                    }

                    var cache = entry.TryGetSubTreeNode();
                    if (cache == null)
                    {
                        // this entry doesn't have sub tree cache
                        continue;
                    }

                    // ask its sub tree cache
                    cache.AppendChecksumObjects(builder, checksums, cancellationToken);
                    if (checksums.Count == 0)
                    {
                        // we found all
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

                foreach (var entry in _cache.Values)
                {
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

            public Task<TAsset> GetOrCreateAssetAsync<TKey, TValue, TAsset>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TAsset>> valueGetterAsync, CancellationToken cancellationToken)
                where TKey : class
                where TAsset : Asset
            {
                return GetOrCreateChecksumObjectAsync(key, value, kind, valueGetterAsync, cancellationToken);
            }

            protected void AppendChecksumObjects(
                ImmutableDictionary<Checksum, ChecksumObject>.Builder builder,
                HashSet<Checksum> checksums,
                int itemCount,
                IEnumerable<Checksum> itemChecksums,
                Func<Checksum, ChecksumObject> itemGetter, CancellationToken cancellationToken)
            {
                using (var removed = Creator.CreateList<Checksum>())
                {
                    foreach (var checksum in GetChecksums(checksums.Count, checksums, itemCount, itemChecksums))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var checksumObject = itemGetter(checksum);
                        if (checksumObject != null && checksums.Contains(checksum))
                        {
                            // found entry
                            builder[checksum] = checksumObject;
                            removed.Object.Add(checksum);

                            if (removed.Object.Count == checksums.Count)
                            {
                                break;
                            }
                        }
                    }

                    checksums.ExceptWith(removed.Object);
                }
            }

            private IEnumerable<Checksum> GetChecksums(int count1, IEnumerable<Checksum> checksums1, int count2, IEnumerable<Checksum> checksums2)
            {
                return count1 < count2 ? checksums1 : checksums2;
            }

            private void AppendChecksumObjects(
                ImmutableDictionary<Checksum, ChecksumObject>.Builder builder, HashSet<Checksum> checksums, ChecksumObjectCache entry, CancellationToken cancellationToken)
            {
                AppendChecksumObjects(builder, checksums, entry.Count, entry.GetChecksums(), c => GetItem(entry, c), cancellationToken);
            }

            private ChecksumObject GetItem(ChecksumObjectCache entry, Checksum checksum)
            {
                ChecksumObject checksumObject;
                if (entry.TryGetValue(checksum, out checksumObject))
                {
                    return checksumObject;
                }

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
