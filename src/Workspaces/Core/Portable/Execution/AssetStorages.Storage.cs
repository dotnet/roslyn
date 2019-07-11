// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            private ConcurrentDictionary<Checksum, CustomAsset> _additionalAssets;

            public Storage(SolutionState solutionState)
            {
                SolutionState = solutionState;

                _serializer = SolutionState.Workspace.Services.GetService<ISerializerService>();
            }

            public SolutionState SolutionState { get; }

            public void AddAdditionalAsset(CustomAsset asset)
            {
                LazyInitialization.EnsureInitialized(ref _additionalAssets, s_additionalAssetsCreator);

                _additionalAssets.TryAdd(asset.Checksum, asset);
            }

            public RemotableData TryGetRemotableData(Checksum checksum, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);

                var syncObject = finder.Find(checksum);
                if (syncObject != null)
                {
                    return syncObject;
                }

                CustomAsset asset = null;
                if (_additionalAssets?.TryGetValue(checksum, out asset) == true)
                {
                    return asset;
                }

                // this cache has no reference to the given checksum
                return null;
            }

            public void AppendRemotableData(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, RemotableData> result, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);

                finder.Append(searchingChecksumsLeft, result);
                if (searchingChecksumsLeft.Count == 0)
                {
                    // there is no checksum left to find
                    return;
                }

                // storage has extra data that we need to search as well
                if (_additionalAssets == null)
                {
                    // checksum looks like belong to global asset
                    return;
                }

                AppendRemotableDataFromAdditionalAssets(result, searchingChecksumsLeft, cancellationToken);
            }

            private void AppendRemotableDataFromAdditionalAssets(
                Dictionary<Checksum, RemotableData> result, HashSet<Checksum> searchingChecksumsLeft, CancellationToken cancellationToken)
            {
                // this will iterate through candidate checksums to see whether that checksum exists in both
                // checksum set we are currently searching for and checksums current node contains
                using var removed = Creator.CreateList<Checksum>();

                // we have 2 sets of checksums. one we are searching for and ones this node contains.
                // we only need to iterate one of them to see this node contains what we are looking for.
                // so, we check two set and use one that has smaller number of checksums.
                foreach (var checksum in GetSmallerChecksumList(searchingChecksumsLeft.Count, searchingChecksumsLeft, _additionalAssets.Count, _additionalAssets.Keys))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // if checksumGetter return null for given checksum, that means current node doesn't have given checksum
                    var checksumObject = GetRemotableDataFromAdditionalAssets(checksum);
                    if (checksumObject != null && searchingChecksumsLeft.Contains(checksum))
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

            private RemotableData GetRemotableDataFromAdditionalAssets(Checksum checksum)
            {
                if (_additionalAssets.TryGetValue(checksum, out var asset))
                {
                    return asset;
                }

                // given checksum doesn't exist in this additional assets. but will exist
                // in one of tree nodes/additional assets/global assets
                return null;
            }

            private static IEnumerable<Checksum> GetSmallerChecksumList(int count1, IEnumerable<Checksum> checksums1, int count2, IEnumerable<Checksum> checksums2)
            {
                // return smaller checksum list from given two list
                return count1 < count2 ? checksums1 : checksums2;
            }

            private static string CreateLogMessage<T>(T key, string kind)
            {
                return $"{kind} - {GetLogInfo(key) ?? "unknown"}";
            }

            private static string GetLogInfo<T>(T key)
                => key switch
                {
                    SolutionState solutionState => solutionState.FilePath,
                    ProjectState projectState => projectState.FilePath,
                    DocumentState documentState => documentState.FilePath,
                    _ => "no detail",
                };
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

            public RemotableData Find(Checksum checksum)
            {
                using (var checksumPool = Creator.CreateChecksumSet(SpecializedCollections.SingletonEnumerable(checksum)))
                using (var resultPool = Creator.CreateResultSet())
                {
                    Append(checksumPool.Object, resultPool.Object);

                    if (resultPool.Object.Count == 1)
                    {
                        var kv = resultPool.Object.First();

                        Contract.ThrowIfFalse(checksum == kv.Key);
                        return SolutionAsset.Create(kv.Key, kv.Value, _serializer);
                    }
                }

                return null;
            }

            public void Append(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, RemotableData> result)
            {
                using var resultPool = Creator.CreateResultSet();

                Append(searchingChecksumsLeft, resultPool.Object);

                foreach (var kv in resultPool.Object)
                {
                    result[kv.Key] = SolutionAsset.Create(kv.Key, kv.Value, _serializer);
                }
            }

            private void Append(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, object> result)
            {
                // only solution with checksum can be in asset storage
                Contract.ThrowIfFalse(_state.TryGetStateChecksums(out var stateChecksums));

                stateChecksums.Find(_state, searchingChecksumsLeft, result, _cancellationToken);
            }
        }
    }
}
