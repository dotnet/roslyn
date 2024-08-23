// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Serialization;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A set of <see cref="IsolatedAnalyzerReference"/>s and their associated shadow copy loader (which has its own <see
/// cref="AssemblyLoadContext"/>).  As long as something is keeping this set alive, the ALC will be kept alive.  Once
/// this set is dropped, the loader will be explicitly <see cref="IDisposable.Dispose"/>'d in its finalizer.
/// </summary>
internal sealed partial class IsolatedAnalyzerReferenceSet
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
    private static readonly Dictionary<Checksum, WeakReference<IsolatedAnalyzerReferenceSet>> s_checksumToReferenceSet = [];

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

    private IsolatedAnalyzerReferenceSet(
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
    ~IsolatedAnalyzerReferenceSet()
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

    public static async partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ImmutableArray<AnalyzerReference> references,
        SolutionServices solutionServices,
        CancellationToken cancellationToken)
    {
        if (references.Length == 0)
            return [];

        var serializerService = solutionServices.GetRequiredService<ISerializerService>();
        var analyzerChecksums = ChecksumCache.GetOrCreateChecksumCollection(references, serializerService, cancellationToken);

        return await CreateIsolatedAnalyzerReferencesAsync(
            useAsync,
            analyzerChecksums,
            solutionServices,
            () => Task.FromResult(references),
            cancellationToken).ConfigureAwait(false);
    }

    public static async partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ChecksumCollection analyzerChecksums,
        SolutionServices solutionServices,
        Func<Task<ImmutableArray<AnalyzerReference>>> getReferencesAsync,
        CancellationToken cancellationToken)
    {
        if (analyzerChecksums.Children.Length == 0)
            return [];

        var checksum = analyzerChecksums.Checksum;

        // First, see if these were already computed and stored.
        using (useAsync
            ? await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false)
            : s_isolatedReferenceSetGate.DisposableWait(cancellationToken))
        {
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }
        }

        // Not already stored.  Fetch the actual references.
        var analyzerReferences = await getReferencesAsync().ConfigureAwait(false);
        var assemblyLoaderProvider = solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        using (useAsync
           ? await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false)
           : s_isolatedReferenceSetGate.DisposableWait(cancellationToken))
        {
            // Check again to see if another thread beat us.
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }

            isolatedAssemblyReferenceSet = new IsolatedAnalyzerReferenceSet(analyzerReferences, assemblyLoaderProvider);

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
