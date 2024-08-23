// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// A set of <see cref="IsolatedAnalyzerReference"/>s and their associated shadow copy loader (which has its own <see
/// cref="AssemblyLoadContext"/>).  As long as something is keeping this set alive, the ALC will be kept alive.  Once
/// this set is dropped, the loader will be explicitly <see cref="IDisposable.Dispose"/>'d in its finalizer.
/// </summary>
internal sealed class IsolatedAssemblyReferenceSet
{
    /// <summary>
    /// Gate around <see cref="s_checksumToReferenceSet"/> to ensure it is only accessed and updated atomically.
    /// </summary>
    private static readonly SemaphoreSlim s_isolatedReferenceSetGate = new(initialCount: 1);

    /// <summary>
    /// Mapping from checksum for a particular set of assembly references, to the dedicated ALC and actual assembly
    /// references corresponding to it.  As long as it is alive, we will try to reuse what is in memory.  But once it is
    /// dropped from memory, we'll clean things up and produce a new one.
    /// </summary>
    private static readonly Dictionary<Checksum, WeakReference<IsolatedAssemblyReferenceSet>> s_checksumToReferenceSet = [];

    private static int s_sweepCount = 0;

    /// <summary>
    /// Final set of <see cref="AnalyzerReference"/> instances that will be passed through the workspace down to the compiler.
    /// </summary>
    public ImmutableArray<AnalyzerReference> AnalyzerReferences { get; }

    /// <summary>
    /// Dedicated loader with its own dedicated ALC that all analyzer references will load their <see
    /// cref="System.Reflection.Assembly"/>s within.
    /// </summary>
    private readonly IAnalyzerAssemblyLoaderInternal _shadowCopyLoader;

    private IsolatedAssemblyReferenceSet(
        ImmutableArray<AnalyzerReference> initialReferences,
        IAnalyzerAssemblyLoaderProvider provider)
    {
        // Now make a fresh loader that uses that ALC that will ensure these references are properly isolated.
        _shadowCopyLoader = provider.CreateNewShadowCopyLoader();

        var builder = new FixedSizeArrayBuilder<AnalyzerReference>(initialReferences.Length);
        foreach (var initialReference in initialReferences)
        {
            // If we already have an analyzer reference isolated to another ALC.  Fish out its underlying reference so
            // we can rewrap it for the new ALC we're creating.  We don't want to continually wrap layers of isolated
            // objects.
            var analyzerReference = initialReference is IsolatedAnalyzerReference isolatedReference
                ? isolatedReference.UnderlyingAnalyzerReference
                : initialReference;

            // If we have an existing file reference, make a new one with a different loader/ALC.  Otherwise, it's some
            // other analyzer reference we don't understand (like an in-memory one created in tests).
            var finalReference = analyzerReference is AnalyzerFileReference analyzerFileReference
                ? new IsolatedAnalyzerReference(this, new AnalyzerFileReference(analyzerFileReference.FullPath, _shadowCopyLoader))
                : initialReference;

            builder.Add(finalReference);
        }

        this.AnalyzerReferences = builder.MoveToImmutable();
    }

    /// <summary>
    /// When the last reference this to this reference set finally goes away, it is safe to unload our loader+ALC.
    /// </summary>
    ~IsolatedAssemblyReferenceSet()
    {
        _shadowCopyLoader.Dispose();
    }

    private static void GarbageCollectReleaseReferences_NoLock()
    {
        Contract.ThrowIfTrue(s_isolatedReferenceSetGate.CurrentCount != 0);

        // When we've done some reasonable number of mutations to the dictionary, we'll do a sweep to see if there are
        // entries we can remove.
        if (++s_sweepCount % 128 == 0)
            return;

        using var _ = ArrayBuilder<Checksum>.GetInstance(out var checksumsToRemove);

        foreach (var (checksum, weakReference) in s_checksumToReferenceSet)
        {
            if (!weakReference.TryGetTarget(out var referenceSet) ||
                referenceSet is null)
            {
                checksumsToRemove.Add(checksum);
            }
        }

        foreach (var checksum in checksumsToRemove)
            s_checksumToReferenceSet.Remove(checksum);
    }

    /// <summary>
    /// Given a checksum for a set of analyzer references, fetches the existing ALC-isolated set of them if already
    /// present in this process.  Otherwise, this fetches the raw serialized analyzer references from the host side,
    /// then creates and caches an isolated set on the OOP side to hold onto them, passing out that isolated set of
    /// references to be used by the caller (normally to be stored in a solution snapshot).
    /// </summary>
    public static async ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        ImmutableArray<AnalyzerReference> references,
        ISerializerService serializerService,
        IAnalyzerAssemblyLoaderProvider assemblyLoaderProvider,
        CancellationToken cancellationToken)
    {
        if (references.Length == 0)
            return [];

        var analyzerChecksums = ChecksumCache.GetOrCreateChecksumCollection(references, serializerService, cancellationToken);
        var checksum = analyzerChecksums.Checksum;

        // First, see if these were already computed and stored.
        using (await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }

            isolatedAssemblyReferenceSet = new IsolatedAssemblyReferenceSet(references, assemblyLoaderProvider);

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
    }
}

#endif
