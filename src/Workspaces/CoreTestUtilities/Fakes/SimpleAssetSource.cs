// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        private readonly ISerializerService _serializerService;
        private readonly IReadOnlyDictionary<Checksum, object> _map;

        public SimpleAssetSource(ISerializerService serializerService, IReadOnlyDictionary<Checksum, object> map)
        {
            _serializerService = serializerService;
            _map = map;
        }

        public ValueTask<ImmutableArray<(Checksum, object)>> GetAssetsAsync(
            Checksum solutionChecksum, ISet<Checksum> checksums, ISerializerService deserializerService, CancellationToken cancellationToken)
        {
            var results = new List<(Checksum, object)>();

            foreach (var checksum in checksums)
            {
                if (_map.TryGetValue(checksum, out var data))
                {
                    using var stream = new MemoryStream();
                    using var context = new SolutionReplicationContext();

                    using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                    {
                        _serializerService.Serialize(data, writer, context, cancellationToken);
                    }

                    stream.Position = 0;
                    using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);
                    var asset = deserializerService.Deserialize<object>(data.GetWellKnownSynchronizationKind(), reader, cancellationToken);
                    Contract.ThrowIfTrue(asset is null);
                    results.Add((checksum, asset));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(checksum);
                }
            }

            return ValueTaskFactory.FromResult(results.ToImmutableArray());
        }
    }
}
