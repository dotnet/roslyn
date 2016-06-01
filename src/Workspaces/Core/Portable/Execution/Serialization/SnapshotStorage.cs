// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal class SnapshotStorages
    {
        private readonly Serializer _serializer;
        private readonly ConcurrentDictionary<SolutionSnapshot, MySnapshotStorage> _snapshots;

        public SnapshotStorages(Serializer serializer)
        {
            _serializer = serializer;
            _snapshots = new ConcurrentDictionary<SolutionSnapshot, MySnapshotStorage>(concurrencyLevel: 2, capacity: 10);
        }

        public SnapshotStorage CreateSnapshotStorage(Solution solution)
        {
            return new MySnapshotStorage(this, solution);
        }

        public async Task<ChecksumObject> GetChecksumObjectAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            foreach (var storage in _snapshots.Values)
            {
                var asset = storage.TryGetChecksumObject(checksum, cancellationToken);
                if (asset != null)
                {
                    return asset;
                }
            }

            // it looks like asset doesn't exist. checksumObject held seems to be already released.
            foreach (var storage in _snapshots.Values)
            {
                var snapshotBuilder = new SnapshotBuilder(_serializer, storage, rebuild: true);

                // rebuild whole asset for this solution
                await snapshotBuilder.BuildAsync(storage.Solution, cancellationToken).ConfigureAwait(false);

                var checksumObject = storage.TryGetChecksumObject(checksum, cancellationToken);
                if (checksumObject != null)
                {
                    return checksumObject;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(checksum);
        }

        private Entry TryGetChecksumObjectEntry(object key, CancellationToken cancellationToken)
        {
            foreach (var storage in _snapshots.Values)
            {
                var etrny = storage.TryGetChecksumObjectEntry(key, cancellationToken);
                if (etrny != null)
                {
                    return etrny;
                }
            }

            return null;
        }

        public void RegisterSnapshot(SolutionSnapshot snapshot, SnapshotStorage storage)
        {
            // duplicates are not allowed, there can be multiple snapshots to same solution, so no ref counting.
            Contract.ThrowIfFalse(_snapshots.TryAdd(snapshot, (MySnapshotStorage)storage));
        }

        public void UnregisterSnapshot(SolutionSnapshot snapshot)
        {
            // calling it multiple times for same snapshot is not allowed.
            MySnapshotStorage dummy;
            Contract.ThrowIfFalse(_snapshots.TryRemove(snapshot, out dummy));
        }

        private sealed class MySnapshotStorage : SnapshotStorage
        {
            private readonly SnapshotStorages _owner;

            // this cache can be moved into object itself if we decide to do so. especially objects used in solution.
            // this cache is basically attached property. again, this is cache since we can always rebuild these.
            private readonly ConditionalWeakTable<object, Entry> _objectToChecksumObjectCache;

            // this is cache since we can always rebuild checksum objects from solution. so this affects perf not functionality.
            // this cache exists so that we can skip things building if we can avoid.
            private readonly ConcurrentDictionary<Checksum, ChecksumObject> _checksumToChecksumObjectCache;

            public MySnapshotStorage(SnapshotStorages owner, Solution solution) :
                base(solution)
            {
                _owner = owner;

                _objectToChecksumObjectCache = new ConditionalWeakTable<object, Entry>();
                _checksumToChecksumObjectCache = new ConcurrentDictionary<Checksum, ChecksumObject>(concurrencyLevel: 2, capacity: solution.ProjectIds.Count * 30);
            }

            public ChecksumObject TryGetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                ChecksumObject checksumObject;
                if (_checksumToChecksumObjectCache.TryGetValue(checksum, out checksumObject))
                {
                    return checksumObject;
                }

                return null;
            }

            public Entry TryGetChecksumObjectEntry(object key, CancellationToken cancellationToken)
            {
                Entry entry;
                if (_objectToChecksumObjectCache.TryGetValue(key, out entry))
                {
                    return entry;
                }

                return null;
            }

            public override async Task<TChecksumObject> GetOrCreateHierarchicalChecksumObjectAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TChecksumObject>> valueGetterAsync, bool rebuild,
                CancellationToken cancellationToken)
            {
                if (rebuild)
                {
                    // force to re-create all sub checksum objects
                    // save newly created one
                    SaveAndReturn(key, await valueGetterAsync(value, kind, cancellationToken).ConfigureAwait(false));
                }

                return await GetOrCreateChecksumObjectAsync(key, value, kind, valueGetterAsync, cancellationToken).ConfigureAwait(false);
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
                Contract.ThrowIfNull(key);

                // ask myself
                ChecksumObject checksumObject;
                var entry = TryGetChecksumObjectEntry(key, cancellationToken);
                if (entry != null && entry.TryGetValue(kind, out checksumObject))
                {
                    return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
                }

                // ask owner
                entry = _owner.TryGetChecksumObjectEntry(key, cancellationToken);
                if (entry == null || !entry.TryGetValue(kind, out checksumObject))
                {
                    // owner doesn't have it, create one.
                    checksumObject = await valueGetterAsync(value, kind, cancellationToken).ConfigureAwait(false);
                }

                // record local copy (reference) and return it.
                // REVIEW: we can go ref count route rather than this (local copy). but then we need to make sure there is no leak.
                //         for now, we go local copy route since overhead is small (just duplicated reference pointer), but reduce complexity a lot.
                //
                //         also, assumption is, most of time, out of proc has most of data they need already cached. so opimization is done for common
                //         case where creating snapshot is cheap. but rebuilding one is expensive relatively. that is why we do not copy over
                //         all sub tree. (or we can change data structure to tree so copying over root copy over all sub elements)
                return (TChecksumObject)SaveAndReturn(key, checksumObject, entry);
            }

            private ChecksumObject SaveAndReturn(object key, ChecksumObject checksumObject, Entry entry = null)
            {
                // create new entry if it is not already given
                entry = entry ?? new Entry(checksumObject);
                entry = _objectToChecksumObjectCache.GetValue(key, _ => entry);

                var saved = entry.Add(checksumObject);
                _checksumToChecksumObjectCache.TryAdd(saved.Checksum, saved);
                return saved;
            }
        }

        private class Entry
        {
            private readonly ChecksumObject _checksumObject;
            private ConcurrentDictionary<string, ChecksumObject> _lazyMap;

            public Entry(ChecksumObject checksumObject)
            {
                _checksumObject = checksumObject;
            }

            public ChecksumObject Add(ChecksumObject checksumObject)
            {
                if (_checksumObject.Kind == checksumObject.Kind)
                {
                    // we already have one
                    Contract.Requires(_checksumObject.Checksum.Equals(checksumObject.Checksum));
                    return _checksumObject;
                }

                EnsureLazyMap();
                if (_lazyMap.TryAdd(checksumObject.Kind, checksumObject))
                {
                    // just added new one
                    return checksumObject;
                }

                // there is existing one.
                return _lazyMap[checksumObject.Kind];
            }

            public bool TryGetValue(string kind, out ChecksumObject checksumObject)
            {
                if (_checksumObject.Kind == kind)
                {
                    checksumObject = _checksumObject;
                    return true;
                }

                if (_lazyMap != null)
                {
                    return _lazyMap.TryGetValue(kind, out checksumObject);
                }

                checksumObject = null;
                return false;
            }

            private void EnsureLazyMap()
            {
                if (_lazyMap != null)
                {
                    return;
                }

                // we have multiple entries. create lazy map
                lock (_checksumObject)
                {
                    if (_lazyMap != null)
                    {
                        return;
                    }

                    _lazyMap = new ConcurrentDictionary<string, ChecksumObject>(concurrencyLevel: 2, capacity: 1);
                }
            }
        }
    }

    internal abstract class SnapshotStorage
    {
        public readonly Solution Solution;

        protected SnapshotStorage(Solution solution)
        {
            Solution = solution;
        }

        public abstract Task<TChecksumObject> GetOrCreateHierarchicalChecksumObjectAsync<TKey, TValue, TChecksumObject>(
            TKey key, TValue value, string kind,
            Func<TValue, string, CancellationToken, Task<TChecksumObject>> valueGetterAsync,
            bool rebuild,
            CancellationToken cancellationToken)
            where TKey : class
            where TChecksumObject : HierarchicalChecksumObject;


        public abstract Task<TAsset> GetOrCreateAssetAsync<TKey, TValue, TAsset>(
            TKey key, TValue value, string kind,
            Func<TValue, string, CancellationToken, Task<TAsset>> valueGetterAsync,
            CancellationToken cancellationToken)
            where TKey : class
            where TAsset : Asset;
    }
}
