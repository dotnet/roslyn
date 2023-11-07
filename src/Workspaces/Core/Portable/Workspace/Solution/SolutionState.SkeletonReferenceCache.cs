// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    /// <summary>
    /// Caches the skeleton references produced for a given project/compilation under the varying <see
    /// cref="MetadataReferenceProperties"/> it might be referenced by.  Skeletons are used in the compilation tracker
    /// to allow cross-language project references with live semantic updating between VB/C# and vice versa.
    /// Specifically, in a cross language case we will build a skeleton ref for the referenced project and have the
    /// referrer use that to understand its semantics.
    /// <para>
    /// This approach works, but has the caveat that live cross-language semantics are only possible when the skeleton
    /// assembly can be built.  This should always be the case for correct code, but it may not be the case for code
    /// with errors depending on if the respective language compiler is resilient to those errors or not. In that case
    /// though where the skeleton cannot be built, this type provides mechanisms to fallback to the last successfully
    /// built skeleton so that a somewhat reasonable experience can be maintained.  If we failed to do this and instead
    /// returned nothing, a user would find that practically all semantic experiences that depended on that particular
    /// project would fail or be seriously degraded (e.g. diagnostics).  To that end, it's better to limp along with
    /// stale date, then barrel on ahead with no data.
    /// </para>
    /// <para>
    /// The implementation works by keeping metadata references around associated with a specific <see
    /// cref="VersionStamp"/> for a project. As long as the <see cref="Project.GetDependentSemanticVersionAsync"/> for
    /// that project is the same, then all the references of it can be reused.  When an <see
    /// cref="ICompilationTracker"/> forks itself, it will also <see cref="Clone"/> this, allowing previously computed
    /// references to be used by later forks. However, this means that later forks (esp. ones that fail to produce a
    /// skeleton, or which produce a skeleton for different semantics) will not leak backward to a prior <see
    /// cref="ProjectState"/>, causing it to see a view of the world inapplicable to its current snapshot.  A downside
    /// of this is that if a fork happens to a compilation tracker *prior* to the skeleton for it being computed, then
    /// when the skeleton is actually produced it won't be shared forward.  In practice the hope is that this is rare,
    /// and that eventually the compilation trackers will have computed the skeleton and will be able to pass it forward
    /// from that point onwards.
    /// </para>
    /// <para>
    /// The cached data we compute is associated with a particular compilation-tracker.  Because of this, once we
    /// compute the skeleton information for that tracker, we hold onto it for as long as the tracker is itself alive.
    /// The presumption here is that once created, it will likely be needed in the future as well as there will still be
    /// downstream projects of different languages that reference this.  The only time this won't hold true is if there
    /// was a cross language p2p ref, but then it gets removed from the solution.  However, this sort of change should
    /// be rare in a solution, so it's unlikely to happen much, and the only negative is holding onto a little bit more
    /// memory.
    /// </para>
    /// </summary>
    private partial class SkeletonReferenceCache
    {
        private static readonly EmitOptions s_metadataOnlyEmitOptions = new(metadataOnly: true);

        /// <summary>
        /// Static conditional mapping from a compilation to the skeleton set produced for it.  This is valuable for a
        /// couple of reasons. First, a compilation tracker may fork, but produce the same compilation.  As such, we
        /// want to get the same skeleton set for it.  Second, consider the following scenario:
        /// <list type="number">
        /// <item>Project A is referenced by projects B and C (both have a different language than A).</item>
        /// <item>Producing the compilation for 'B' produces the compilation for 'A' which produces the skeleton that 'B' references.</item>
        /// <item>B's compilation is released and then GC'ed.</item> 
        /// <item>Producing the compilation for 'C' needs the skeleton from 'A'</item>
        /// </list>
        /// At this point we would not want to re-emit the assembly metadata for A's compilation.  We already did that
        /// for 'B', and it can be enormously expensive to do so again.  So as long as A's compilation lives, we really
        /// want to keep it's skeleton cache around.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, AsyncLazy<SkeletonReferenceSet?>> s_compilationToSkeletonSet = new();

        /// <summary>
        /// Lock around <see cref="_version"/> and <see cref="_skeletonReferenceSet"/> to ensure they are updated/read 
        /// in an atomic fashion.
        /// </summary>
        private readonly object _stateGate = new();

        /// <summary>
        /// The <see cref="Project.GetDependentSemanticVersionAsync"/> version of the project that the
        /// <see cref="_skeletonReferenceSet"/> corresponds to.
        /// </summary>
        private VersionStamp? _version;

        /// <summary>
        /// Mapping from metadata-reference-properties to the actual metadata reference for them.
        /// </summary>
        private SkeletonReferenceSet? _skeletonReferenceSet;

        public SkeletonReferenceCache()
            : this(version: null, skeletonReferenceSet: null)
        {
        }

        private SkeletonReferenceCache(
            VersionStamp? version,
            SkeletonReferenceSet? skeletonReferenceSet)
        {
            _version = version;
            _skeletonReferenceSet = skeletonReferenceSet;
        }

        /// <summary>
        /// Produces a copy of the <see cref="SkeletonReferenceCache"/>, allowing forks of <see cref="ProjectState"/> to
        /// reuse <see cref="MetadataReference"/>s when their dependent semantic version matches ours.  In the case where
        /// the version is different, then the clone will attempt to make a new skeleton reference for that version.  If it
        /// succeeds, it will use that.  If it fails however, it can still use our skeletons.
        /// </summary>
        public SkeletonReferenceCache Clone()
        {
            lock (_stateGate)
            {
                // pass along the best version/reference-set we computed for ourselves.  That way future ProjectStates
                // can use this data if either the version changed, or they weren't able to build a skeleton for themselves.
                // By passing along a copy we ensure that if they have a different version, they'll end up producing a new
                // SkeletonReferenceSet where they'll store their own data in which will not affect prior ProjectStates.
                return new SkeletonReferenceCache(_version, _skeletonReferenceSet);
            }
        }

        public MetadataReference? TryGetAlreadyBuiltMetadataReference(MetadataReferenceProperties properties)
            => _skeletonReferenceSet?.GetOrCreateMetadataReference(properties);

        public async Task<MetadataReference?> GetOrBuildReferenceAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            MetadataReferenceProperties properties,
            CancellationToken cancellationToken)
        {
            var version = await compilationTracker.GetDependentSemanticVersionAsync(solution, cancellationToken).ConfigureAwait(false);
            var referenceSet = await TryGetOrCreateReferenceSetAsync(
                compilationTracker, solution, version, cancellationToken).ConfigureAwait(false);
            if (referenceSet == null)
                return null;

            return referenceSet.GetOrCreateMetadataReference(properties);
        }

        private async Task<SkeletonReferenceSet?> TryGetOrCreateReferenceSetAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            // First, just see if we have cached a reference set that is complimentary with the version of the project
            // being passed in.  If so, we can just reuse what we already computed before.
            if (TryReadSkeletonReferenceSetAtThisVersion(version, out var referenceSet))
                return referenceSet;

            // okay, we don't have anything cached with this version. so create one now.

            var currentSkeletonReferenceSet = await CreateSkeletonReferenceSetAsync(compilationTracker, solution, cancellationToken).ConfigureAwait(false);

            lock (_stateGate)
            {
                // If we successfully created the metadata storage, then create the new set that points to it.
                // if we didn't, that's ok too, we'll just say that for this requested version, that we can
                // return any prior computed reference set (including 'null' if we've never successfully made
                // a skeleton).
                if (currentSkeletonReferenceSet != null)
                    _skeletonReferenceSet = currentSkeletonReferenceSet;

                _version = version;

                return _skeletonReferenceSet;
            }
        }

        private static async Task<SkeletonReferenceSet?> CreateSkeletonReferenceSetAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            CancellationToken cancellationToken)
        {
            // It's acceptable for this computation to be something that multiple calling threads may hit at once.  The
            // implementation inside the compilation tracker does an async-wait on a an internal semaphore to ensure 
            // only one thread actually does the computation and the rest wait.
            var compilation = await compilationTracker.GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);
            var services = solution.Services;

            // note: computing the assembly metadata is actually synchronous.  However, this ensures we don't have N
            // threads blocking on a lazy to compute the work.  Instead, we'll only occupy one thread, while any
            // concurrent requests asynchronously wait for that work to be done.

            var lazy = s_compilationToSkeletonSet.GetValue(compilation,
                compilation => AsyncLazy.Create(
                    cancellationToken => Task.FromResult(CreateSkeletonSet(services, compilation, cancellationToken))));

            return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        private static SkeletonReferenceSet? CreateSkeletonSet(
            SolutionServices services, Compilation compilation, CancellationToken cancellationToken)
        {
            var storage = TryCreateMetadataStorage(services, compilation, cancellationToken);
            if (storage == null)
                return null;

            var metadata = AssemblyMetadata.CreateFromStream(storage.ReadStream(cancellationToken), leaveOpen: false);

            // read in the stream and pass ownership of it to the metadata object.  When it is disposed it will dispose
            // the stream as well.
            return new SkeletonReferenceSet(
                metadata,
                compilation.AssemblyName,
                new DeferredDocumentationProvider(compilation));
        }

        private bool TryReadSkeletonReferenceSetAtThisVersion(VersionStamp version, out SkeletonReferenceSet? result)
        {
            lock (_stateGate)
            {
                // if we're asking about the same version as we've cached, then return whatever have (regardless of
                // whether it succeeded or not.
                if (version == _version)
                {
                    result = _skeletonReferenceSet;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static ITemporaryStreamStorageInternal? TryCreateMetadataStorage(SolutionServices services, Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var logger = services.GetService<IWorkspaceTestLogger>();

            try
            {
                logger?.Log($"Beginning to create a skeleton assembly for {compilation.AssemblyName}...");

                using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_EmitMetadataOnlyImage, cancellationToken))
                {
                    using var stream = SerializableBytes.CreateWritableStream();

                    var emitResult = compilation.Emit(stream, options: s_metadataOnlyEmitOptions, cancellationToken: cancellationToken);

                    if (emitResult.Success)
                    {
                        logger?.Log($"Successfully emitted a skeleton assembly for {compilation.AssemblyName}");

                        var temporaryStorageService = services.GetRequiredService<ITemporaryStorageServiceInternal>();
                        var storage = temporaryStorageService.CreateTemporaryStreamStorage();

                        stream.Position = 0;
                        storage.WriteStream(stream, cancellationToken);

                        return storage;
                    }

                    if (logger != null)
                    {
                        logger.Log($"Failed to create a skeleton assembly for {compilation.AssemblyName}:");

                        foreach (var diagnostic in emitResult.Diagnostics)
                        {
                            logger.Log("  " + diagnostic.GetMessage());
                        }
                    }

                    // log emit failures so that we can improve most common cases
                    Logger.Log(FunctionId.MetadataOnlyImage_EmitFailure, KeyValueLogMessage.Create(m =>
                    {
                        // log errors in the format of
                        // CS0001:1;CS002:10;...
                        var groups = emitResult.Diagnostics.GroupBy(d => d.Id).Select(g => $"{g.Key}:{g.Count()}");
                        m["Errors"] = string.Join(";", groups);
                    }));

                    return null;
                }
            }
            finally
            {
                logger?.Log($"Done trying to create a skeleton assembly for {compilation.AssemblyName}");
            }
        }
    }
}
