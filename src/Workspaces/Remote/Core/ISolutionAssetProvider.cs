// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Brokered service.
    /// </summary>
    internal interface ISolutionAssetProvider
    {
        /// <summary>
        /// Streams serialized assets into the given stream.
        /// </summary>
        ValueTask GetAssetsAsync(PipeWriter pipeWriter, Checksum solutionChecksum, Checksum[] checksums, CancellationToken cancellationToken);
    }
}
