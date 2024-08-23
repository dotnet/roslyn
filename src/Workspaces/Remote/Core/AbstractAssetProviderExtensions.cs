// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal static partial class AbstractAssetProviderExtensions
{
    public static Task GetAssetsAsync<TAsset>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, HashSet<Checksum> checksums, CancellationToken cancellationToken)
    {
        return assetProvider.GetAssetsAsync<TAsset, VoidResult>(
            assetPath, checksums, callback: null, arg: default, cancellationToken);
    }

    public static Task GetAssetsAsync<T>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, CancellationToken cancellationToken)
    {
        return assetProvider.GetAssetsAsync<T, VoidResult>(
            assetPath, checksums, callback: null, arg: default, cancellationToken);
    }

    public static async Task GetAssetsAsync<T, TArg>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken)
    {
        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksumSet);
#if NET
        checksumSet.EnsureCapacity(checksums.Children.Length);
#endif
        checksumSet.AddAll(checksums.Children);

        await assetProvider.GetAssetsAsync(assetPath, checksumSet, callback, arg, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns an array of assets, corresponding to all the checksums found in the given <paramref name="checksums"/>.
    /// The assets will be returned in the order corresponding to their checksum in <paramref name="checksums"/>.
    /// </summary>
    public static async Task<ImmutableArray<T>> GetAssetsArrayAsync<T>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, CancellationToken cancellationToken) where T : class
    {
        // Note: nothing stops 'checksums' from having multiple identical checksums in it.  First, collapse this down to
        // a set so we're only asking about unique checksums.
        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksumSet);
#if NET
        checksumSet.EnsureCapacity(checksums.Children.Length);
#endif
        checksumSet.AddAll(checksums.Children);

        using var _2 = PooledDictionary<Checksum, T>.GetInstance(out var checksumToAsset);

        await assetProvider.GetAssetHelper<T>().GetAssetsAsync(
            assetPath, checksumSet,
            // Calling .Add here is safe.  As checksum-set is a unique set of checksums, we'll never have collions here.
            static (checksum, asset, checksumToAsset) => checksumToAsset.Add(checksum, asset),
            checksumToAsset,
            cancellationToken).ConfigureAwait(false);

        // Note: GetAssetsAsync will only succeed if we actually found all our assets (it crashes otherwise).  So we can
        // just safely assume we can index into checksumToAsset here.
        Contract.ThrowIfTrue(checksumToAsset.Count != checksumSet.Count);

        // The result of GetAssetsArrayAsync wants the returned assets to be in the exact order of the checksums that
        // were in 'checksums'.  So now fetch the assets in that order, even if we found them in an entirely different
        // order.
        var result = new FixedSizeArrayBuilder<T>(checksums.Children.Length);
        foreach (var checksum in checksums.Children)
            result.Add(checksumToAsset[checksum]);

        return result.MoveToImmutable();
    }
}
