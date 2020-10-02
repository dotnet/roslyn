// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provides assets given their checksums.
    /// </summary>
    internal interface IAssetSource
    {
        Task<ImmutableArray<(Checksum, object)>> GetAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken);

        // TODO: remove (https://github.com/dotnet/roslyn/issues/43477)
        Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken);
    }
}
