// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
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
    /// is the same, then all the references of it can be reused.  When a <see cref="ProjectState"/> forks itself, it 
    /// will also <see cref="Clone"/> this, allowing previously computed references to be used by later forks.  However,
    /// this means that later forks (esp. ones that fail to produce a skeleton, or which produce a skeleton for different
    /// semantics) will not leak backward to a prior <see cref="ProjectState"/>, causing it to see a view of the world
    /// inapplicable to its current snapshot.
    /// </summary>
    internal class CachedSkeletonReferences
    {
        /// <summary>
        /// Mapping from compilation instance to metadata-references for it.  Safe to use as a static CWT as anyone 
        /// with a reference to this compilation (and the same <see cref="MetadataReferenceProperties"/> is allowed
        /// to reference it using the same <see cref="MetadataReference"/>.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, SkeletonReferenceSet> s_compilationToReferenceMap = new();

        private readonly ProjectId _projectId;

        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// The version of the project that these <see cref="_skeletonReferenceSet"/> correspond to.
        /// </summary>
        private VersionStamp? _version;

        /// <summary>
        /// Mapping from metadata-reference-properties to the actual metadata reference for them.
        /// </summary>
        private SkeletonReferenceSet? _skeletonReferenceSet;

        public CachedSkeletonReferences(ProjectId projectId)
            : this(projectId, version: null, skeletonReferenceSet: null)
        {
        }

        private CachedSkeletonReferences(
            ProjectId projectId,
            VersionStamp? version,
            SkeletonReferenceSet? skeletonReferenceSet)
        {
            _projectId = projectId;
            _version = version;
            _skeletonReferenceSet = skeletonReferenceSet;
        }

        public CachedSkeletonReferences Clone()
        {
            lock (_gate)
            {
                // pass along the best version/reference-set we computed for ourselves.  That way future ProjectStates
                // can use this data if either the version changed, or they weren't able to build a skeleton for themselves.
                // By passing along a copy we ensure that if they have a different version, they'll end up producing a new
                // SkeletonReferenceSet where they'll store their own data in which will not affect prior ProjectStates.
                return new CachedSkeletonReferences(_projectId, _version, _skeletonReferenceSet);
            }
        }

        public async Task<MetadataReference> GetOrBuildReferenceAsync(
            Workspace workspace,
            MetadataReferenceProperties properties,
            Compilation finalCompilation,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            // First see if we already have a cached reference for either finalCompilation or for projectReference.
            // If we have one for the latter, we'll make sure that it's version matches what we're asking for before
            // returning it.
            workspace.LogTestMessage($"Looking to see if we already have a skeleton assembly for {_projectId} before we build one...");
            var reference = await TryGetReferenceAsync(workspace, properties, finalCompilation, version, cancellationToken).ConfigureAwait(false);
            if (reference != null)
            {
                workspace.LogTestMessage($"A reference was found {_projectId} so we're skipping the build.");
                return reference;
            }

            // okay, we don't have one. so create one now.

            // first, prepare image
            // * NOTE * image is cancellable, do not create it inside of conditional weak table.
            var service = workspace.Services.GetService<ITemporaryStorageService>();
            var image = MetadataOnlyImage.Create(workspace, service, finalCompilation, cancellationToken);

            if (image.IsEmpty)
            {
                // unfortunately, we couldn't create one. see if we have one from previous compilation., it might be
                // out-of-date big time, but better than nothing.
                reference = await TryGetReferenceAsync(workspace, properties, finalCompilation, version: null, cancellationToken).ConfigureAwait(false);
                if (reference != null)
                {
                    workspace.LogTestMessage($"We failed to create metadata so we're using the one we just found from an earlier version.");
                    return reference;
                }
            }

            // We either had an image, or we didn't have an image and we didn't have a previous value we could reuse.
            // Just store this image against this version for us, and any future ProjectStates that fork from us.

            var tempReferenceSet = new SkeletonReferenceSet(image);
            var referenceSet = s_compilationToReferenceMap.GetValue(finalCompilation, _ => tempReferenceSet);
            if (tempReferenceSet != referenceSet)
            {
                // someone else has beaten us. 
                // let image go eagerly. otherwise, finalizer in temporary storage will take care of it
                image.Cleanup();
            }

            // Store this for ourselves, and for any project states that clone from us from this point onwards.
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _version = version;
                _skeletonReferenceSet = referenceSet;
            }

            return await referenceSet.GetMetadataReferenceAsync(finalCompilation, properties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to get the <see cref="MetadataReference"/> with the given <paramref name="properties"/>
        /// for the <see cref="Compilation"/> <paramref name="finalOrDeclarationCompilation"/>.
        /// </summary>
        public Task<MetadataReference?> TryGetReferenceAsync(
            Workspace workspace,
            MetadataReferenceProperties properties,
            Compilation finalOrDeclarationCompilation,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            return TryGetReferenceAsync(workspace, properties, finalOrDeclarationCompilation, (VersionStamp?)version, cancellationToken);
        }

        /// <remarks>
        /// <inheritdoc cref="TryGetReferenceAsync(Workspace, MetadataReferenceProperties, Compilation, VersionStamp, CancellationToken)"/>.
        /// If <paramref name="version"/> is <see langword="null"/>, any <see cref="MetadataReference"/> for that
        /// project may be returned, even if it doesn't correspond to that compilation.  This is useful in error tolerance
        /// cases as building a skeleton assembly may easily fail.  In that case it's better to use the last successfully
        /// built skeleton than just have no semantic information for that project at all.
        /// </remarks> 
        private async Task<MetadataReference?> TryGetReferenceAsync(
            Workspace workspace,
            MetadataReferenceProperties properties,
            Compilation finalOrDeclarationCompilation,
            VersionStamp? version,
            CancellationToken cancellationToken)
        {
            // if we have one from snapshot cache, use it. it will make sure same compilation will get same metadata reference always.
            if (s_compilationToReferenceMap.TryGetValue(finalOrDeclarationCompilation, out var referenceSet))
            {
                workspace.LogTestMessage($"Found already cached metadata in {nameof(s_compilationToReferenceMap)} for the exact compilation");
                return await referenceSet.GetMetadataReferenceAsync(
                    finalOrDeclarationCompilation, properties, cancellationToken).ConfigureAwait(false);
            }

            // okay, now use version based cache that can live multiple compilation as long as there is no semantic changes.

            var result = await TryGetReferenceAsync(properties, finalOrDeclarationCompilation, version, cancellationToken).ConfigureAwait(false);
            if (result != null)
                workspace.LogTestMessage($"Found already cached metadata for the branch and version {version}");

            return result;
        }

        private async Task<MetadataReference?> TryGetReferenceAsync(
            MetadataReferenceProperties properties,
            Compilation finalOrDeclarationCompilation,
            VersionStamp? version,
            CancellationToken cancellationToken)
        {
            SkeletonReferenceSet set;
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_skeletonReferenceSet != null &&
                    (version == null || _version == version))
                {
                    // record it to snapshot based cache.
                    set = s_compilationToReferenceMap.GetValue(finalOrDeclarationCompilation, _ => _skeletonReferenceSet);
                }
                else
                {
                    return null;
                }
            }

            return await set.GetMetadataReferenceAsync(finalOrDeclarationCompilation, properties, cancellationToken).ConfigureAwait(false);
        }

        private class SkeletonReferenceSet
        {
            private readonly SemaphoreSlim _gate = new(initialCount: 1);

            // use WeakReference so we don't keep MetadataReference's alive if they are not being consumed
            private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _metadataReferences = new();
            private readonly MetadataOnlyImage _image;

            public SkeletonReferenceSet(MetadataOnlyImage image)
            {
                _image = image;
            }

            public async Task<MetadataReference> GetMetadataReferenceAsync(
                Compilation compilation, MetadataReferenceProperties properties, CancellationToken cancellationToken)
            {
                // lookup first and eagerly return cached value if we have it.
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_metadataReferences.TryGetValue(properties, out var weakMetadata) &&
                        weakMetadata.TryGetTarget(out var metadataReference))
                    {
                        return metadataReference;
                    }
                }

                // otherwise, create the metadata outside of the lock, and then try to assign it if no one else beat us
                {
                    var metadataReference = _image.CreateReference(properties.Aliases, properties.EmbedInteropTypes, new DeferredDocumentationProvider(compilation));
                    var weakMetadata = new WeakReference<MetadataReference>(metadataReference);

                    using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (_metadataReferences.TryGetValue(properties, out var innherWeakMetadata) &&
                            weakMetadata.TryGetTarget(out var innerMetadataReference))
                        {
                            // someone beat us to writing this.
                            return innerMetadataReference;
                        }

                        _metadataReferences[properties] = weakMetadata;
                    }

                    return metadataReference;
                }
            }
        }
    }
}
