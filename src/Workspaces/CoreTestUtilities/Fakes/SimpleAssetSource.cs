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

namespace Microsoft.CodeAnalysis.Remote.Testing;

/// <summary>
/// provide asset from given map at the creation
/// </summary>
internal sealed class SimpleAssetSource(ISerializerService serializerService, IReadOnlyDictionary<Checksum, object> map) : IAssetSource
{
    public ValueTask<ImmutableArray<object>> GetAssetsAsync(
        Checksum solutionChecksum, AssetHint assetHint, ImmutableArray<Checksum> checksums, ISerializerService deserializerService, CancellationToken cancellationToken)
    {
        var results = new List<object>();

        foreach (var checksum in checksums)
        {
            Contract.ThrowIfFalse(map.TryGetValue(checksum, out var data));

            using var stream = new MemoryStream();
            using var context = new SolutionReplicationContext();

            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                serializerService.Serialize(data, writer, context, cancellationToken);
            }

            stream.Position = 0;
            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);
            var asset = deserializerService.Deserialize<object>(data.GetWellKnownSynchronizationKind(), reader, cancellationToken);
            Contract.ThrowIfNull(asset);
            results.Add(asset);
        }

        return ValueTaskFactory.FromResult(results.ToImmutableArray());
    }
}
