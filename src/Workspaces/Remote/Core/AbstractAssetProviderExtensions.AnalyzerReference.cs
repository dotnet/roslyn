// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote;

internal static partial class AbstractAssetProviderExtensions
{
#if NET


#endif

    /// <summary>
    /// Given a checksum for a set of analyzer references, fetches the existing ALC-isolated set of them if already
    /// present in this process.  Otherwise, this fetches the raw serialized analyzer references from the host side,
    /// then creates and caches an isolated set on the OOP side to hold onto them, passing out that isolated set of
    /// references to be used by the caller (normally to be stored in a solution snapshot).
    /// </summary>
    public static async ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        this AbstractAssetProvider assetProvider,
        AssetPath assetPath,
        ChecksumCollection analyzerReferencesChecksum,
        IAnalyzerAssemblyLoaderProvider assemblyLoaderProvider,
        CancellationToken cancellationToken)
    {
        if (analyzerReferencesChecksum.Children.Length == 0)
            return [];

#if NET

        var checksum = analyzerReferencesChecksum.Checksum;

        // First, see if these were already computed and stored.
        using (await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }
        }

#endif

        // Otherwise, fetch the raw analyzer references from the host side.
        var serializedReferences = await assetProvider.GetAssetsArrayInternalAsync<AnalyzerReference>(
            assetPath, analyzerReferencesChecksum, cancellationToken).ConfigureAwait(false);

        // Absolutely no AnalyzerFileReferences should have come through here.  We should only have
        // SerializedAnalyzerReferences, as well as any in-memory references made by tests.
        Contract.ThrowIfTrue(serializedReferences.Any(static r => r is AnalyzerFileReference), $"Should not have gotten an {nameof(AnalyzerFileReference)}");

#if NET

        Contract.ThrowIfTrue(serializedReferences.Any(static r => r is IsolatedAnalyzerReference), $"Should not have gotten an {nameof(IsolatedAnalyzerReference)}");

        // Take the new set of references we've gotten and create a dedicated set of AnalyzerReferences with
        // their own ALC that they can cleanly load (and unload) from.
        using (await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // Check again if another thread beat us.
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }

            isolatedAssemblyReferenceSet = new IsolatedAssemblyReferenceSet(serializedReferences, assemblyLoaderProvider);

            if (weakIsolatedReferenceSet is null)
            {
                weakIsolatedReferenceSet = new(null!);
                s_checksumToReferenceSet[checksum] = weakIsolatedReferenceSet;
            }

            weakIsolatedReferenceSet.SetTarget(isolatedAssemblyReferenceSet);

            // Do some cleaning up of old dictionary entries that are no longer in use.
            GarbageCollectReleaseReferences_NoLock();

            return isolatedAssemblyReferenceSet.AnalyzerReferences;
        }

#else

        // Assembly load contexts not supported here.
        var shadowCopyLoader = assemblyLoaderProvider.SharedShadowCopyLoader;
        var builder = new FixedSizeArrayBuilder<AnalyzerReference>(serializedReferences.Length);

        foreach (var analyzerReference in serializedReferences)
        {
            builder.Add(analyzerReference is SerializerService.SerializedAnalyzerReference serializedAnalyzerReference
                ? new AnalyzerFileReference(serializedAnalyzerReference.FullPath, shadowCopyLoader)
                : analyzerReference);
        }

        return builder.MoveToImmutable();

#endif
    }
}
