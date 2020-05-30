// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD2_0
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of asset storage
    /// </summary>
    internal partial class AssetStorages
    {
        /// <summary>
        /// map from solution checksum scope to its associated asset storage
        /// </summary>
        private readonly ConcurrentDictionary<int, Storage> _storages;

        public AssetStorages()
        {
            _storages = new ConcurrentDictionary<int, Storage>(concurrencyLevel: 2, capacity: 10);
        }

        public static Storage CreateStorage(SolutionState solutionState)
            => new Storage(solutionState);

        public async ValueTask<RemotableData?> GetRemotableDataAsync(int scopeId, Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return RemotableData.Null;
            }

            // search snapshots we have
            var storage = _storages[scopeId];
            var remotableData = await storage.TryGetRemotableDataAsync(checksum, cancellationToken).ConfigureAwait(false);
            if (remotableData != null)
            {
                return remotableData;
            }

            // if it reached here, it means things get cancelled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things cancelled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return null;
        }

        public async ValueTask<IReadOnlyDictionary<Checksum, RemotableData>> GetRemotableDataAsync(int scopeId, IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using var searchingChecksumsLeft = Creator.CreateChecksumSet(checksums);

            var numberOfChecksumsToSearch = searchingChecksumsLeft.Object.Count;
            var result = new Dictionary<Checksum, RemotableData>(numberOfChecksumsToSearch);

            // check nil case
            if (searchingChecksumsLeft.Object.Remove(Checksum.Null))
            {
                result[Checksum.Null] = RemotableData.Null;
            }

            // search checksum trees we have
            var storage = _storages[scopeId];

            await storage.AppendRemotableDataAsync(searchingChecksumsLeft.Object, result, cancellationToken).ConfigureAwait(false);
            if (result.Count == numberOfChecksumsToSearch)
            {
                // no checksum left to find
                Debug.Assert(searchingChecksumsLeft.Object.Count == 0);
                return result;
            }

            // if it reached here, it means things get cancelled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things cancelled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        public void RegisterSnapshot(int scopeId, AssetStorages.Storage storage)
        {
            // duplicates are not allowed, there can be multiple snapshots to same solution, so no ref counting.
            if (!_storages.TryAdd(scopeId, storage))
            {
                // this should make failure more explicit
                FailFast.OnFatalException(new Exception("who is adding same snapshot?"));
            }
        }

        public void UnregisterSnapshot(int scopeId)
        {
            // calling it multiple times for same snapshot is not allowed.
            if (!_storages.TryRemove(scopeId, out _))
            {
                // this should make failure more explicit
                FailFast.OnFatalException(new Exception("who is removing same snapshot?"));
            }
        }

        public async ValueTask<RemotableData?> TestOnly_GetRemotableDataAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            foreach (var (scopeId, _) in _storages)
            {
                var data = await GetRemotableDataAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false);
                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
