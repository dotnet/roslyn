// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
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
    /// Caches the skeleton references produced for a given project/compilation under the varying
    /// <see cref="MetadataReferenceProperties"/> it might be referenced by.  Skeletons are used in the compilation
    /// tracker to allow cross-language project references with live semantic updating between VB/C# and vice versa.
    /// Specifically, in a cross language case we will build a skeleton ref for the referenced project and have the
    /// referrer use that to understand its semantics.
    /// <para/>
    /// This approach works, but has the caveat that live cross-language semantics are only possible when the 
    /// skeleton assembly can be built.  This should always be the case for correct code, but it may not be the
    /// case for code with errors depending on if the respective language compiler is resilient to those errors or not.
    /// In that case though where the skeleton cannot be built, this type provides mechanisms to fallback to the last
    /// successfully built skeleton so that a somewhat reasonable experience can be maintained.  If we failed to do this
    /// and instead returned nothing, a user would find that practically all semantic experiences that depended on
    /// that particular project would fail or be seriously degraded (e.g. diagnostics).  To that end, it's better to
    /// limp along with stale date, then barrel on ahead with no data.
    /// <para/>
    /// The implementation works by keeping metadata references around associated with a specific <see cref="VersionStamp"/>
    /// for a project. As long as the <see cref="Project.GetDependentSemanticVersionAsync"/> for that project
    /// is the same, then all the references of it can be reused.  When an <see cref="ICompilationTracker"/> forks
    /// itself, it  will also <see cref="Clone"/> this, allowing previously computed references to be used by later forks.
    /// However, this means that later forks (esp. ones that fail to produce a skeleton, or which produce a skeleton for 
    /// different semantics) will not leak backward to a prior <see cref="ProjectState"/>, causing it to see a view of the world
    /// inapplicable to its current snapshot.
    /// </summary>
    private partial class SkeletonReferenceCache
    {
        /// <summary>
        /// Version number we mix into the checksums we create to ensure that if our serialization format changes we
        /// automatically will fail 
        /// </summary>
        private static readonly Checksum s_persistenceVersion = Checksum.Create("1");

        private static readonly EmitOptions s_metadataOnlyEmitOptions = new(metadataOnly: true);

        /// <summary>
        /// Lock we take before emitting metadata.  Metadata emit is extremely expensive.  So we want to avoid cases
        /// where N threads come in and try to get the skeleton for a particular project.  This way they will instead
        /// yield if something else is computing and will then use the single instance computed once one thread succeeds.
        /// </summary>
        private static readonly SemaphoreSlim _emitGate = new(initialCount: 1);

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

        public async Task<MetadataReference?> GetOrBuildReferenceAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            MetadataReferenceProperties properties,
            CancellationToken cancellationToken)
        {
            var version = await compilationTracker.GetDependentSemanticVersionAsync(solution, cancellationToken).ConfigureAwait(false);
            var referenceSet = await TryGetOrBuildReferenceSetAsync(
                compilationTracker, solution, version, cancellationToken).ConfigureAwait(false);
            return referenceSet?.GetMetadataReference(properties);
        }

        private async Task<SkeletonReferenceSet?> TryGetOrBuildReferenceSetAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            // First, just see if we have cached a reference set that is complimentary with the version of the project
            // being passed in.  If so, we can just reuse what we already computed before.
            if (TryGetSkeletonReferenceSetMatchingThisVersion(version, out var referenceSet))
                return referenceSet;

            // okay, we don't have anything cached with this version. so create one now.  Note: this is expensive
            // so ensure only one thread is doing hte work to actually make the compilation and emit it.
            using (await _emitGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // after taking the gate, another thread may have succeeded.  See if we can use their version if so:
                if (TryGetSkeletonReferenceSetMatchingThisVersion(version, out referenceSet))
                    return referenceSet;

                // Ok, first thread to get in and actually do this work.  Build the compilation and try to emit it.
                // Regardless of if we succeed or fail, store this result so this only happens once.

                referenceSet = await TryReadOrBuildSkeletonReferenceSetAsync(compilationTracker, solution, cancellationToken).ConfigureAwait(false);

                lock (_stateGate)
                {
                    _skeletonReferenceSet = referenceSet;
                    _version = version;

                    return referenceSet;
                }
            }

            bool TryGetSkeletonReferenceSetMatchingThisVersion(VersionStamp version, out SkeletonReferenceSet? result)
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
        }

        private async Task<SkeletonReferenceSet?> TryReadOrBuildSkeletonReferenceSetAsync(
            ICompilationTracker compilationTracker, SolutionState solution, CancellationToken cancellationToken)
        {
            var workspace = solution.Workspace;
            var project = compilationTracker.ProjectState;

            // First, try to read in from the persistence layer if a previous sessions stored a value there.
            // Mix in our persistence version with project checksum so that we get a different checksum
            // if our format changes and we won't collide with prior written date with a different format.
            var checksum = Checksum.Create(
                s_persistenceVersion,
                await compilationTracker.GetDependentChecksumAsync(solution, cancellationToken).ConfigureAwait(false));

            var referenceSet = await SkeletonReferenceSet.TryReadFromPersistentStorageAsync(
                solution, project, checksum, cancellationToken).ConfigureAwait(false);
            if (referenceSet != null)
                return referenceSet;

            // If not, actually go and build the entire compilation and return a wrapper around that.
            var compilation = await compilationTracker.GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);

            var temporaryStorageService = workspace.Services.GetRequiredService<ITemporaryStorageService>();
            var peStreamStorage = temporaryStorageService.CreateTemporaryStreamStorage(cancellationToken);
            var xmlDocumentationStreamStorage = temporaryStorageService.CreateTemporaryStreamStorage(cancellationToken);

            var success = await TryEmitMetadataAsync(
                workspace, compilation, peStreamStorage, xmlDocumentationStreamStorage, cancellationToken).ConfigureAwait(false);

            // If we successfully emitted the skeleton, then create the new set that points to it.
            // if we didn't, that's ok too we just reuse what we have.  We'll also write this out
            // against this current checksum so that future host sessions will still be able to use this.
            referenceSet = success
                ? new SkeletonReferenceSet(peStreamStorage, xmlDocumentationStreamStorage, compilation.AssemblyName)
                : _skeletonReferenceSet;

            if (referenceSet != null)
            {
                await referenceSet.WriteToPersistentStorageAsync(
                    solution, project, checksum, cancellationToken).ConfigureAwait(false);
            }

            return referenceSet;
        }

        /// <summary>
        /// Returns <see langword="true"/> on success.
        /// </summary>
        private static async Task<bool> TryEmitMetadataAsync(
            Workspace workspace,
            Compilation compilation,
            ITemporaryStreamStorage peStreamStorage,
            ITemporaryStreamStorage xmlDocumentationStreamStorage,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            workspace.LogTestMessage($"Beginning to create a skeleton assembly for {compilation.AssemblyName}...");

            using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_EmitMetadataOnlyImage, cancellationToken))
            {
                using var peStream = SerializableBytes.CreateWritableStream();
                using var xmlDocumentationStream = SerializableBytes.CreateWritableStream();

                // note: cloning compilation so we don't retain all the generated symbols after its emitted.
                // * REVIEW * is cloning clone p2p reference compilation as well?
                var emitResult = compilation.Clone().Emit(
                    peStream: peStream,
                    xmlDocumentationStream: xmlDocumentationStream,
                    options: s_metadataOnlyEmitOptions,
                    cancellationToken: cancellationToken);

                if (emitResult.Success)
                {
                    workspace.LogTestMessage($"Successfully emitted a skeleton assembly for {compilation.AssemblyName}");

                    peStream.Position = 0;
                    await peStreamStorage.WriteStreamAsync(peStream, cancellationToken).ConfigureAwait(false);

                    xmlDocumentationStream.Position = 0;
                    await xmlDocumentationStreamStorage.WriteStreamAsync(xmlDocumentationStream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    workspace.LogTestMessage($"Failed to create a skeleton assembly for {compilation.AssemblyName}:");

                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        workspace.LogTestMessage("  " + diagnostic.GetMessage());
                    }

                    // log emit failures so that we can improve most common cases
                    Logger.Log(FunctionId.MetadataOnlyImage_EmitFailure, KeyValueLogMessage.Create(m =>
                    {
                        // log errors in the format of
                        // CS0001:1;CS002:10;...
                        var groups = emitResult.Diagnostics.GroupBy(d => d.Id).Select(g => $"{g.Key}:{g.Count()}");
                        m["Errors"] = string.Join(";", groups);
                    }));
                }

                return emitResult.Success;
            }
        }
    }
}
