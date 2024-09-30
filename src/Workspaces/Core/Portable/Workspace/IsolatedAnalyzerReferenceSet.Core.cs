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
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A set of <see cref="IsolatedAnalyzerFileReference"/>s and their associated shadow copy loader (which has its own
/// <see cref="AssemblyLoadContext"/>).  As long as something is keeping this set alive, the ALC will be kept alive.
/// Once this set is dropped, the loader will be explicitly <see cref="IDisposable.Dispose"/>'d in its finalizer.
/// </summary>
internal sealed partial class IsolatedAnalyzerReferenceSet
{
    private static readonly ObjectPool<Dictionary<string, Guid>> s_pathToMvidMapPool = new(() => new(SolutionState.FilePathComparer));

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

    /// <summary>
    /// The current isolated reference set we're trying to use to load analyzers in.  We'll keep using the same set
    /// until we run into a conflict that prevents it from being used.  At that point we'll create a new set and use
    /// that one from that point on (and so on).  The old sets will stay alive as long as any AnalyzerReference (or
    /// ISourceGenerator or DiagnosticAnalyzer from it) is alive.  Once all of those are garbage collected, the set
    /// itself can be collected.  At that point it will release it's assembly load context, freeing everything.
    /// </summary>
    /// <remarks>
    /// To determine if we have a conflict, we keep track of the mvid of each <see cref="AnalyzerFileReference"/> when
    /// the set was created.  When trying to reuse the set, we see if any of the references we now have has a different
    /// mvid from that creation point.  If so, we have a conflict and we make a new set.
    /// </remarks>
    private static IsolatedAnalyzerReferenceSet? s_lastCreatedAnalyzerReferenceSet;

    private static int s_sweepCount = 0;

    /// <summary>
    /// Dedicated loader with its own dedicated ALC that all analyzer references will load their <see
    /// cref="System.Reflection.Assembly"/>s within.
    /// </summary>
    private readonly IAnalyzerAssemblyLoaderInternal _shadowCopyLoader;

    /// <summary>
    /// Mapping from <see cref="AnalyzerFileReference.FullPath"/> to the mvid for that reference with this isolated
    /// reference set.  As long as the references we see at those paths have the same mvids, we'll keep using this 
    /// set instance.
    /// </summary>
    private readonly Dictionary<string, Guid> _analyzerFileReferencePathToMvid = [];

    /// <summary>
    /// Mapping from synchronization checksum to the isolated analyzer references created for them.  Used to help oop
    /// synchronization retrieve the same set if multiple projects have the same analyzer references (a common case).
    /// </summary>
    private readonly Dictionary<Checksum, ImmutableArray<AnalyzerReference>> _analyzerReferences = [];

    private IsolatedAnalyzerReferenceSet(
        IAnalyzerAssemblyLoaderProvider provider)
    {
        // Make a fresh loader that uses that ALC that will ensure these references are properly isolated.
        _shadowCopyLoader = provider.CreateNewShadowCopyLoader();
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
        Contract.ThrowIfTrue(s_isolatedReferenceSetGate.CurrentCount != 0, "Lock must be held");

        // When we've done some reasonable number of mutations to the dictionary, we'll do a sweep to see if there are
        // entries we can remove.
        //
        // Note: the value 128 was chosen with absolutely no data.  It was to avoid doing linear sweeps on every change,
        // while also still running reasonably often to clear out old entries.
        //
        // Note: clearing out entries isn't critical.  It's really just a KeyValuePair<Checksum, WeakRef(null)>.  So
        // they aren't really large at all.  But it seemed nice to ensure that the dictionary doesn't grow in an
        // unbounded fashion, even if the entries are small.
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

    private ImmutableArray<AnalyzerReference> GetAnalyzerReferences(Checksum checksum)
        => _analyzerReferences[checksum];

    private static AnalyzerReference GetUnderlyingAnalyzerReference(AnalyzerReference initialReference)
        => initialReference is IsolatedAnalyzerFileReference isolatedReference
            ? isolatedReference.UnderlyingAnalyzerFileReference
            : initialReference;

    private void AddReferences(
        Checksum checksum,
        ImmutableArray<AnalyzerReference> references,
        Dictionary<string, Guid> filePathToMvid)
    {
        Contract.ThrowIfTrue(_analyzerReferences.ContainsKey(checksum));
        Contract.ThrowIfTrue(s_isolatedReferenceSetGate.CurrentCount != 0, "Lock must be held");

        var builder = new FixedSizeArrayBuilder<AnalyzerReference>(references.Length);
        foreach (var initialReference in references)
        {
            // If we already have an analyzer reference isolated to another ALC.  Fish out its underlying reference so
            // we can rewrap it for the new ALC we're creating.  We don't want to continually wrap layers of isolated
            // objects.
            var analyzerReference = GetUnderlyingAnalyzerReference(initialReference);

            // If we have an existing file reference, make a new one with a different loader/ALC.  Otherwise, it's some
            // other analyzer reference we don't understand (like an in-memory one created in tests).
            var finalReference = analyzerReference is AnalyzerFileReference { FullPath: var fullPath }
                ? new IsolatedAnalyzerFileReference(this, new AnalyzerFileReference(fullPath, _shadowCopyLoader))
                : initialReference;

            builder.Add(finalReference);
        }

        _analyzerReferences.Add(checksum, builder.MoveToImmutable());

        // Ensure we know about all the mvids of these analyzer references as well.  As long as they don't change, we
        // can keep reusing this isolated set.
        foreach (var (filePath, mvid) in filePathToMvid)
        {
            Contract.ThrowIfTrue(HasConflict(filePath, mvid));
            _analyzerFileReferencePathToMvid[filePath] = mvid;
        }
    }

    private bool HasConflicts(Dictionary<string, Guid> filePathToMvid)
    {
        foreach (var (filePath, mvid) in filePathToMvid)
        {
            if (HasConflict(filePath, mvid))
                return true;
        }

        return false;
    }

    private bool HasConflict(string filePath, Guid mvid)
        => _analyzerFileReferencePathToMvid.TryGetValue(filePath, out var existingMvid) && existingMvid != mvid;

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

        // Note: this method will end up fetching or creating an IsolatedAssemblyReferenceSet for this checksum.  
        // We'll then return the AnalyzerReferences from within it.  These AnalyzerReferences (which will normally all
        // be IsolatedAnalyzerFileReferences) will themselves root the IsolatedAssemblyReferenceSet, as will all the
        // DiagnosticAnalyzers and ISourceGenerators returned down the line from the IsolatedAnalyzerFileReferences.

        // First, see if these were already computed and stored.
        using (useAsync
            ? await s_isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false)
            : s_isolatedReferenceSetGate.DisposableWait(cancellationToken))
        {
            if (s_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.GetAnalyzerReferences(checksum);
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
                return isolatedAssemblyReferenceSet.GetAnalyzerReferences(checksum);
            }

            // This set of references have not been computed yet.  We have three options:
            //
            // 1. These are the very first time we're seeing any references.  Create a fresh isolated set, and add these new
            //    reference to it.  New references can also be added to this in the future as long as there are no conflicts
            //    with what's in the set already.
            //
            // 2. We have already created an isolated set.  If these new analyzer references conflict with any in the
            //    current set, we create a new set for these and future references to go into.
            //
            // 3. Otherwise, we have an existing set and it has no conflicts.  Add to it directly.

            // Figure out the mvids for all the analyzer references we're being asked about.
            using var _ = s_pathToMvidMapPool.GetPooledObject(out var pathToMvidMap);
            PopulateFilePathToMvidMap(analyzerReferences, pathToMvidMap);

            // Create initial set if we don't have one.
            s_lastCreatedAnalyzerReferenceSet ??= new(assemblyLoaderProvider);

            // If there's an mvid conflict, create a new set.
            if (s_lastCreatedAnalyzerReferenceSet.HasConflicts(pathToMvidMap))
                s_lastCreatedAnalyzerReferenceSet = new(assemblyLoaderProvider);

            // Now add these references/mvids to the isolated alc.
            s_lastCreatedAnalyzerReferenceSet.AddReferences(checksum, analyzerReferences, pathToMvidMap);
            s_checksumToReferenceSet[checksum] = new(s_lastCreatedAnalyzerReferenceSet);

            // Do some cleaning up of old dictionary entries that are no longer in use.
            GarbageCollectReleaseReferences_NoLock();

            return s_lastCreatedAnalyzerReferenceSet.GetAnalyzerReferences(checksum);
        }

        static void PopulateFilePathToMvidMap(
            ImmutableArray<AnalyzerReference> analyzerReferences,
            Dictionary<string, Guid> pathToMvidMap)
        {
            foreach (var initialReference in analyzerReferences)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                // Can ignore all other analyzer reference types.  This is only about analyzer references changing on disk.
                var analyzerReference = GetUnderlyingAnalyzerReference(initialReference);
                if (analyzerReference is AnalyzerFileReference analyzerFileReference)
                    pathToMvidMap[analyzerFileReference.FullPath] = SerializerService.TryGetAnalyzerFileReferenceMvid(analyzerFileReference);
#pragma warning restore CA1416 // Validate platform compatibility
            }
        }
    }
}

#endif
