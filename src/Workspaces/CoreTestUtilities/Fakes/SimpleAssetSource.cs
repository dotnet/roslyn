// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    public async ValueTask GetAssetsAsync<T, TArg>(
        Checksum solutionChecksum, AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, ISerializerService deserializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
    {
        foreach (var checksum in checksums.Span)
        {
            Contract.ThrowIfFalse(map.TryGetValue(checksum, out var data));

            using var stream = new MemoryStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                serializerService.Serialize(data, writer, cancellationToken);
            }

            stream.Position = 0;
            using var reader = ObjectReader.GetReader(stream, leaveOpen: true);
            var asset = deserializerService.Deserialize(data.GetWellKnownSynchronizationKind(), reader, cancellationToken);
            Contract.ThrowIfNull(asset);
            callback(checksum, (T)asset, arg);
        }
    }
}
