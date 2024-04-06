// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Provides assets given their checksums.
/// </summary>
internal interface IAssetSource
{
    ValueTask<ImmutableArray<object>> GetAssetsAsync(
        Checksum solutionChecksum, AssetHint assetHint, ReadOnlyMemory<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken);
}
