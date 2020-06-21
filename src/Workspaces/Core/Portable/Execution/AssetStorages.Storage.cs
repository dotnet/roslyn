// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            private readonly ISerializerService _serializer;

            public SolutionState SolutionState { get; }

            public Storage(SolutionState solutionState)
            {
                SolutionState = solutionState;
                _serializer = SolutionState.Workspace.Services.GetRequiredService<ISerializerService>();
            }

            public async ValueTask<RemotableData?> TryGetRemotableDataAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);

                var syncObject = await finder.FindAsync(checksum).ConfigureAwait(false);
                if (syncObject != null)
                {
                    return syncObject;
                }

                // this cache has no reference to the given checksum
                return null;
            }

            public async Task AppendRemotableDataAsync(HashSet<Checksum> searchingChecksumsLeft, Dictionary<Checksum, RemotableData> result, CancellationToken cancellationToken)
            {
                var finder = new SolutionChecksumFinder(SolutionState, _serializer, cancellationToken);
                await finder.AppendAsync(searchingChecksumsLeft, result).ConfigureAwait(false);
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
