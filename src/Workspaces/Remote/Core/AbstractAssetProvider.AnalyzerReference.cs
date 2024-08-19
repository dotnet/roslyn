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
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote;

internal abstract partial class AbstractAssetProvider
{
#if NET

    /// <summary>
    /// Gate around <see cref="_checksumToReferenceSet"/> to ensure it is only accessed and updated atomically.
    /// </summary>
    private readonly SemaphoreSlim _isolatedReferenceSetGate = new(initialCount: 1);

    /// <summary>
    /// Mapping from checksum for a particular set of assembly references, to the dedicated ALC and actual assembly
    /// references corresponding to it.  As long as it is alive, we will try to reuse what is in memory.  But once it is
    /// dropped from memory, we'll clean things up and produce a new one.
    /// </summary>
    private readonly Dictionary<Checksum, WeakReference<IsolatedAssemblyReferenceSet>> _checksumToReferenceSet = new();

#endif

    public async ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        AssetPath assetPath,
        ChecksumCollection analyzerReferencesChecksum,
        IAnalyzerAssemblyLoaderProvider analyzerAssemblyLoader,
        CancellationToken cancellationToken)
    {
        var serializedReferences = await this.GetAssetsArrayInternalAsync<AnalyzerReference>(
            assetPath, analyzerReferencesChecksum, cancellationToken).ConfigureAwait(false);

        // Absolutely no AnalyzerFileReferences should have come through here.  We should only have
        // SerializedAnalyzerReferences, as well as any in-memory references made by tests.
        Contract.ThrowIfTrue(serializedReferences.Any(static r => r is AnalyzerFileReference));

        // Take the new set of references we've gotten and create a dedicated set of AnalyzerReferences with
        // their own ALC that they can cleanly load (and unload) from.
        var isolatedAnalyzerReferences = await this.CreateAnalyzerReferencesInIsolatedAssemblyLoadContextAsync(
            analyzerReferencesChecksum.Checksum, serializedReferences, analyzerAssemblyLoader, cancellationToken).ConfigureAwait(false);

        return isolatedAnalyzerReferences;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<ImmutableArray<AnalyzerReference>> CreateAnalyzerReferencesInIsolatedAssemblyLoadContextAsync(
        Checksum checksum,
        ImmutableArray<AnalyzerReference> serializedReferences,
        IAnalyzerAssemblyLoaderProvider analyzerAssemblyLoaderProvider,
        CancellationToken cancellationToken)
    {
#if NET

        using (await _isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }

            isolatedAssemblyReferenceSet = new IsolatedAssemblyReferenceSet(serializedReferences, analyzerAssemblyLoaderProvider);

            if (weakIsolatedReferenceSet is null)
            {
                weakIsolatedReferenceSet = new(null!);
                _checksumToReferenceSet[checksum] = weakIsolatedReferenceSet;
            }

            weakIsolatedReferenceSet.SetTarget(isolatedAssemblyReferenceSet);
            return isolatedAssemblyReferenceSet.AnalyzerReferences;
        }

#else

        // Assembly load contexts not supported here.
        var shadowCopyLoader = analyzerAssemblyLoaderProvider.GetShadowCopyLoader();
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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
