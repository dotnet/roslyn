// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Brokered service.
/// </summary>
internal interface ISolutionAssetProvider
{
    /// <summary>
    /// Streams serialized assets into the given stream.  Assets will be serialized in the exact same order
    /// corresponding to the checksum index in <paramref name="checksums"/>.
    /// </summary>
    /// <param name="pipeWriter">The writer to write the assets into.  Implementations of this method must call<see
    /// cref="PipeWriter.Complete"/> on it (in the event of failure or success).  Failing to do so will lead to hangs on
    /// the code that reads from the corresponding <see cref="PipeReader"/> side of this.</param>
    /// <param name="assetPath">Optional project and document ids to scope the search for checksums down to.  This can
    /// save substantially on performance by avoiding having to search the full solution tree to find matching items for
    /// a particular checksum.</param>
    ValueTask WriteAssetsAsync(
        PipeWriter pipeWriter, Checksum solutionChecksum, AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, CancellationToken cancellationToken);
}
