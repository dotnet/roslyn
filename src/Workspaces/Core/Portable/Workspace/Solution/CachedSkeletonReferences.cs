// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    /// is the same, then all the references of it can be reused.  When an <see cref="SolutionState.ICompilationTracker"/> forks
    /// itself, it  will also <see cref="Clone"/> this, allowing previously computed references to be used by later forks.
    /// However, this means that later forks (esp. ones that fail to produce a skeleton, or which produce a skeleton for 
    /// different semantics) will not leak backward to a prior <see cref="ProjectState"/>, causing it to see a view of the world
    /// inapplicable to its current snapshot.
    /// </summary>
    private partial class CachedSkeletonReferences
    {
        /// <summary>
        /// Mapping from compilation instance to metadata-references for it.  This allows us to associate the same
        /// <see cref="SkeletonReferenceSet"/> to different compilations that may not be the same as the original
        /// compilation we generated the set from.  This allows us to use compilations as keys as long as they're
        /// alive, but also associate the set with new compilations that are generated in the future if the older
        /// compilations were thrown away.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, SkeletonReferenceSet> s_compilationToReferenceMap = new();
        private static readonly EmitOptions s_metadataOnlyEmitOptions = new(metadataOnly: true);

        /// <summary>
        /// Lock we take before emitting metadata.  Metadata emit is extremely expensive.  So we want to avoid cases
        /// where N threads come in and try to get the skeleton for a particular project.  This way they will instead
        /// yield if something else is computing and will then use the single instance computed once one thread succeeds.
        /// </summary>
        private readonly SemaphoreSlim _emitGate = new(initialCount: 1);

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

        public CachedSkeletonReferences()
            : this(version: null, skeletonReferenceSet: null)
        {
        }

        private CachedSkeletonReferences(
            VersionStamp? version,
            SkeletonReferenceSet? skeletonReferenceSet)
        {
            _version = version;
            _skeletonReferenceSet = skeletonReferenceSet;
        }

        /// <summary>
        /// Produces a copy of the <see cref="CachedSkeletonReferences"/>, allowing forks of <see cref="ProjectState"/> to
        /// reuse <see cref="MetadataReference"/>s when their dependent semantic version matches ours.  In the case where
        /// the version is different, then the clone will attempt to make a new skeleton reference for that version.  If it
        /// succeeds, it will use that.  If it fails however, it can still use our skeletons.
        /// </summary>
        public CachedSkeletonReferences Clone()
        {
            lock (_stateGate)
            {
                // pass along the best version/reference-set we computed for ourselves.  That way future ProjectStates
                // can use this data if either the version changed, or they weren't able to build a skeleton for themselves.
                // By passing along a copy we ensure that if they have a different version, they'll end up producing a new
                // SkeletonReferenceSet where they'll store their own data in which will not affect prior ProjectStates.
                return new CachedSkeletonReferences(_version, _skeletonReferenceSet);
            }
        }

        public async Task<MetadataReference?> GetOrBuildReferenceAsync(
            ICompilationTracker compilationTracker,
            SolutionState solution,
            MetadataReferenceProperties properties,
            CancellationToken cancellationToken)
        {
            // First, just see if we have cached a reference set that is complimentary with the version of the project
            // being passed in.  If so, we can just reuse what we already computed before.
            var workspace = solution.Workspace;
            var version = await compilationTracker.GetDependentSemanticVersionAsync(solution, cancellationToken).ConfigureAwait(false);
            var metadataReference = TryGetReferenceSet(version)?.GetMetadataReference(properties);
            if (metadataReference != null)
            {
                workspace.LogTestMessage($"Reusing the already cached skeleton assembly for {compilationTracker.ProjectState.Id}");
                return metadataReference;
            }

            var compilation = await compilationTracker.GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);

            // Didn't have a direct mapping to a reference set.  Compute one for ourselves.
            var referenceSet = await GetOrBuildReferenceSetAsync(workspace, version, compilation, cancellationToken).ConfigureAwait(false);

            // another thread may have come in and beaten us to computing this.  So attempt to actually cache this
            // in the global map.  if it succeeds, use our computed version.  If it fails, use the one the other
            // thread succeeded in storing.
            referenceSet = s_compilationToReferenceMap.GetValue(compilation, _ => referenceSet);

            lock (_stateGate)
            {
                // whoever won, still store this reference set against us with the provided version.
                _version = version;
                _skeletonReferenceSet = referenceSet;
            }

            return referenceSet.GetMetadataReference(properties);
        }

        private async Task<SkeletonReferenceSet> GetOrBuildReferenceSetAsync(
            Workspace workspace,
            VersionStamp version,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var referenceSet = TryGetExistingReferenceSet(version, compilation);
            if (referenceSet != null)
                return referenceSet;

            // okay, we don't have one. so create one now.
            ITemporaryStreamStorage? storage;

            using (await _emitGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // after taking the gate, another thread may have succeeded.  See if we can use their version if so:
                referenceSet = TryGetExistingReferenceSet(version, compilation);
                if (referenceSet != null)
                    return referenceSet;

                storage = TryCreateMetadataStorage(workspace, compilation, cancellationToken);
            }

            if (storage == null)
            {
                // unfortunately, we couldn't create one. see if we have one from previous compilation., it might be
                // out-of-date big time, but better than nothing.
                referenceSet = TryGetReferenceSet(version: null);
                if (referenceSet != null)
                {
                    workspace.LogTestMessage($"We failed to create metadata so we're using the one we just found from an earlier version.");
                    return referenceSet;
                }
            }

            return new SkeletonReferenceSet(storage, compilation.AssemblyName, new DeferredDocumentationProvider(compilation));
        }

        private SkeletonReferenceSet? TryGetExistingReferenceSet(VersionStamp version, Compilation compilation)
        {
            // first, check if we have a direct mapping from this compilation to a reference set. If so, use it.  This
            // ensures the same compilations will get same metadata reference.
            if (s_compilationToReferenceMap.TryGetValue(compilation, out var referenceSet))
                return referenceSet;

            // Then see if we already have a reference set for this version.  if so, we're done and can return that.
            return TryGetReferenceSet(version);
        }

        private static ITemporaryStreamStorage? TryCreateMetadataStorage(Workspace workspace, Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                workspace.LogTestMessage($"Beginning to create a skeleton assembly for {compilation.AssemblyName}...");

                using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_EmitMetadataOnlyImage, cancellationToken))
                {
                    // TODO: make it to use SerializableBytes.WritableStream rather than MemoryStream so that
                    //       we don't allocate anything for skeleton assembly.
                    using var stream = SerializableBytes.CreateWritableStream();
                    // note: cloning compilation so we don't retain all the generated symbols after its emitted.
                    // * REVIEW * is cloning clone p2p reference compilation as well?
                    var emitResult = compilation.Clone().Emit(stream, options: s_metadataOnlyEmitOptions, cancellationToken: cancellationToken);

                    if (emitResult.Success)
                    {
                        workspace.LogTestMessage($"Successfully emitted a skeleton assembly for {compilation.AssemblyName}");

                        var temporaryStorageService = workspace.Services.GetRequiredService<ITemporaryStorageService>();
                        var storage = temporaryStorageService.CreateTemporaryStreamStorage(cancellationToken);

                        stream.Position = 0;
                        storage.WriteStream(stream, cancellationToken);

                        return storage;
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

                        return null;
                    }
                }
            }
            finally
            {
                workspace.LogTestMessage($"Done trying to create a skeleton assembly for {compilation.AssemblyName}");
            }
        }

        /// <summary>
        /// Tries to get the <see cref="SkeletonReferenceSet"/> for this project matching <paramref name="version"/>.
        /// if <paramref name="version"/> is <see langword="null"/>, any cached <see cref="SkeletonReferenceSet"/> 
        /// can be returned, even if it doesn't correspond to that version.  This is useful in error tolerance cases
        /// as building a skeleton assembly may easily fail. In that case it's better to use the last successfully 
        /// built skeleton than just have no semantic information for that project at all.
        /// </summary>
        private SkeletonReferenceSet? TryGetReferenceSet(VersionStamp? version)
        {
            // Otherwise, we don't have a direct mapping stored.  Try to see if the cached reference we have is
            // applicable to this project semantic version.
            lock (_stateGate)
            {
                // if we don't have a skeleton cached, then we have nothing to return.
                if (_skeletonReferenceSet == null)
                    return null;

                // if the caller is requiring a particular semantic version, it much match what we have cached.
                if (version != null && version != _version)
                    return null;

                return _skeletonReferenceSet;
            }
        }

        /// <summary>
        /// Return a metadata reference if we already have a reference-set computed for this particular <paramref name="version"/>.
        /// If a reference already exists for the provided <paramref name="properties"/>, the same instance will be returned.  Otherwise,
        /// a fresh instance will be returned.
        /// </summary>
        public MetadataReference? TryGetReference(VersionStamp version, MetadataReferenceProperties properties)
            => TryGetReferenceSet(version)?.GetMetadataReference(properties);

        private sealed class SkeletonReferenceSet
        {
            /// <summary>
            /// A map to ensure that the streams from the temporary storage service that back the metadata we create stay alive as long
            /// as the metadata is alive.
            /// </summary>
            private static readonly ConditionalWeakTable<AssemblyMetadata, ISupportDirectMemoryAccess> s_lifetime = new();

            private readonly ITemporaryStreamStorage? _storage;
            private readonly string? _assemblyName;

            /// <summary>
            /// The documentation provider used to lookup xml docs for any metadata reference we pass out.  See
            /// docs on <see cref="DeferredDocumentationProvider"/> for why this is safe to hold onto despite it
            /// rooting a compilation internally.
            /// </summary>
            private readonly DeferredDocumentationProvider _documentationProvider;

            /// <summary>
            /// Protection for <see cref="_metadataReferences"/>.
            /// </summary>
            private readonly object _gate = new();

            /// <summary>
            /// Use WeakReference so we don't keep MetadataReference's alive if they are not being consumed. 
            /// Note: if the weak-reference is actuall <see langword="null"/> (not that it points to null),
            /// that means 
            /// </summary>
            private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>?> _metadataReferences = new();

            public SkeletonReferenceSet(
                ITemporaryStreamStorage? storage,
                string? assemblyName,
                DeferredDocumentationProvider documentationProvider)
            {
                _storage = storage;
                _assemblyName = assemblyName;
                _documentationProvider = documentationProvider;
            }

            public MetadataReference? GetMetadataReference(MetadataReferenceProperties properties)
            {
                // lookup first and eagerly return cached value if we have it.
                lock (_gate)
                {
                    if (TryGetExisting_NoLock(properties, out var metadataReference))
                        return metadataReference;
                }

                // otherwise, create the metadata outside of the lock, and then try to assign it if no one else beat us
                {
                    var metadataReference = CreateReference(properties.Aliases, properties.EmbedInteropTypes, _documentationProvider);
                    var weakMetadata = metadataReference == null ? null : new WeakReference<MetadataReference>(metadataReference);

                    lock (_gate)
                    {
                        // see if someone beat us to writing this.
                        if (TryGetExisting_NoLock(properties, out var existingMetadataReference))
                            return existingMetadataReference;

                        _metadataReferences[properties] = weakMetadata;
                    }

                    return metadataReference;
                }

                bool TryGetExisting_NoLock(MetadataReferenceProperties properties, out MetadataReference? metadataReference)
                {
                    metadataReference = null;
                    if (!_metadataReferences.TryGetValue(properties, out var weakMetadata))
                        return false;

                    // If we are pointing at a null-weak reference (not a weak reference that points to null), then we 
                    // know we failed to create the metadata the last time around, and we can shortcircuit immediately,
                    // returning null *with* success to bubble that up.
                    if (weakMetadata == null)
                        return true;

                    return weakMetadata.TryGetTarget(out metadataReference);
                }
            }

            private MetadataReference? CreateReference(ImmutableArray<string> aliases, bool embedInteropTypes, DocumentationProvider documentationProvider)
            {
                if (_storage == null)
                    return null;

                // first see whether we can use native memory directly.
                var stream = _storage.ReadStream();
                AssemblyMetadata metadata;

                if (stream is ISupportDirectMemoryAccess supportNativeMemory)
                {
                    // this is unfortunate that if we give stream, compiler will just re-copy whole content to 
                    // native memory again. this is a way to get around the issue by we getting native memory ourselves and then
                    // give them pointer to the native memory. also we need to handle lifetime ourselves.
                    metadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(supportNativeMemory.GetPointer(), (int)stream.Length));

                    // Tie lifetime of stream to metadata we created. It is important to tie this to the Metadata and not the
                    // metadata reference, as PE symbols hold onto just the Metadata. We can use Add here since we created
                    // a brand new object in AssemblyMetadata.Create above.
                    s_lifetime.Add(metadata, supportNativeMemory);
                }
                else
                {
                    // Otherwise, we just let it use stream. Unfortunately, if we give stream, compiler will
                    // internally copy it to native memory again. since compiler owns lifetime of stream,
                    // it would be great if compiler can be little bit smarter on how it deals with stream.

                    // We don't deterministically release the resulting metadata since we don't know 
                    // when we should. So we leave it up to the GC to collect it and release all the associated resources.
                    metadata = AssemblyMetadata.CreateFromStream(stream);
                }

                return metadata.GetReference(
                    documentation: documentationProvider,
                    aliases: aliases,
                    embedInteropTypes: embedInteropTypes,
                    display: _assemblyName);
            }
        }
    }
}
