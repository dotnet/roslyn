// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of root checksum tree node
    /// </summary>
    internal partial class AssetStorages
    {
        /// <summary>
        /// root tree node of checksum tree
        /// </summary>
        public sealed class Storage
        {
            // cache to remove lambda allocation
            private readonly static Func<ConcurrentDictionary<Checksum, CustomAsset>> s_additionalAssetsCreator = () => new ConcurrentDictionary<Checksum, CustomAsset>(concurrencyLevel: 2, capacity: 10);

            private readonly ISerializerService _serializer;

            // additional assets that is not part of solution but added explicitly
            private ConcurrentDictionary<Checksum, CustomAsset>? _lazyAdditionalAssets;

            public SolutionState SolutionState { get; }

            public Storage(SolutionState solutionState)
            {
                SolutionState = solutionState;
                _serializer = SolutionState.Workspace.Services.GetRequiredService<ISerializerService>();
            }

            public void AddAdditionalAsset(CustomAsset asset)
            {
                LazyInitialization.EnsureInitialized(ref _lazyAdditionalAssets, s_additionalAssetsCreator).TryAdd(asset.Checksum, asset);
            }

            public async ValueTask<RemotableData?> TryGetRemotableDataAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);

                var syncObject = await finder.FindAsync(checksum).ConfigureAwait(false);
                if (syncObject != null)
                {
                    return syncObject;
                }

                if (_lazyAdditionalAssets != null && _lazyAdditionalAssets.TryGetValue(checksum, out var asset))
                {
                    return asset;
                }

                // this cache has no reference to the given checksum
                return null;
            }

            public async Task AppendRemotableDataAsync(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, RemotableData> result, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);

                await finder.AppendAsync(searchingChecksumsLeft, result).ConfigureAwait(false);

                if (searchingChecksumsLeft.Count == 0)
                {
                    // there is no checksum left to find
                    return;
                }

                // storage has extra data that we need to search as well
                if (_lazyAdditionalAssets == null)
                {
                    // checksum belongs to global asset
                    return;
                }

                // this will iterate through candidate checksums to see whether that checksum exists in both
                // checksum set we are currently searching for and checksums current node contains
                using var removed = Creator.CreateList<Checksum>();

                var shorterChecksumList = (searchingChecksumsLeft.Count < _lazyAdditionalAssets.Count) ? searchingChecksumsLeft : _lazyAdditionalAssets.Keys;

                // we have 2 sets of checksums. one we are searching for and ones this node contains.
                // we only need to iterate one of them to see this node contains what we are looking for.
                // so, we check two set and use one that has smaller number of checksums.
                foreach (var checksum in shorterChecksumList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // given checksum doesn't exist in this additional assets. but will exist
                    // in one of tree nodes/additional assets/global assets
                    if (_lazyAdditionalAssets.TryGetValue(checksum, out var checksumObject) &&
                        searchingChecksumsLeft.Contains(checksum))
                    {
                        // found given checksum in current node
                        result[checksum] = checksumObject;
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

        private struct SolutionChecksumFinder
        {
            private readonly SolutionState _state;
            private readonly ISerializerService _serializer;
            private readonly CancellationToken _cancellationToken;

            public SolutionChecksumFinder(SolutionState state, ISerializerService serializer, CancellationToken cancellationToken) : this()
            {
                _state = state;
                _serializer = serializer;
                _cancellationToken = cancellationToken;
            }

            public async ValueTask<RemotableData?> FindAsync(Checksum checksum)
            {
                using var checksumPool = Creator.CreateChecksumSet(SpecializedCollections.SingletonEnumerable(checksum));
                using var resultPool = Creator.CreateResultSet();

                await AppendAsync(checksumPool.Object, resultPool.Object).ConfigureAwait(false);

                if (resultPool.Object.Count == 1)
                {
                    var (resultingChecksum, value) = resultPool.Object.First();
                    Contract.ThrowIfFalse(checksum == resultingChecksum);

                    return new SolutionAsset(checksum, value, _serializer);
                }

                return null;
            }

            public async Task AppendAsync(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, RemotableData> result)
            {
                using var resultPool = Creator.CreateResultSet();

                await AppendAsync(searchingChecksumsLeft, resultPool.Object).ConfigureAwait(false);

                foreach (var (checksum, value) in resultPool.Object)
                {
                    result[checksum] = new SolutionAsset(checksum, value, _serializer);
                }
            }

            private async Task AppendAsync(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, object> result)
            {
                // only solution with checksum can be in asset storage
                Contract.ThrowIfFalse(_state.TryGetStateChecksums(out var stateChecksums));

                await stateChecksums.FindAsync(_state, searchingChecksumsLeft, result, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
