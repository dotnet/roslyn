// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    /// <summary>
    /// provide asset from given map at the creation
    /// </summary>
    internal sealed class SimpleAssetSource : IAssetSource
    {
        private readonly IReadOnlyDictionary<Checksum, object> _map;

        public SimpleAssetSource(IReadOnlyDictionary<Checksum, object> map)
        {
            _map = map;
        }

        public Task<ImmutableArray<(Checksum, object)>> GetAssetsAsync(
            int serviceId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            var results = new List<(Checksum, object)>();

            foreach (var checksum in checksums)
            {
                if (_map.TryGetValue(checksum, out var data))
                {
                    results.Add((checksum, data));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(checksum);
                }
            }

            return Task.FromResult(results.ToImmutableArray());
        }

        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
            => SpecializedTasks.False;
    }
}
