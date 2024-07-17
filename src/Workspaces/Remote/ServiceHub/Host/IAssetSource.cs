// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Provides assets given their checksums.
/// </summary>
internal interface IAssetSource
{
    /// <param name="callback">Will be called back once per checksum in <paramref name="checksums"/> in the exact order of that array.</param>
    ValueTask GetAssetsAsync<T, TArg>(
        Checksum solutionChecksum,
        AssetPath assetPath,
        ReadOnlyMemory<Checksum> checksums,
        ISerializerService serializerService,
        TArg arg,
        Action<Checksum, T, TArg> callback,
        CancellationToken cancellationToken);
}
