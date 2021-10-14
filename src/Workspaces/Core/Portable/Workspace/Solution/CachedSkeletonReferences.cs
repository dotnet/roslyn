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
    /// This approach works, but has the caveat that live cross language semantics are only possible when the 
    /// skeleton assembly can be built.  This should always be the case for correct code, but it may not be the
    /// case for code with errors depending on if the respective language compilat is unable to generate the skeleton
    /// in the presence of those errors.  In that case though, this type provides mechanisms to fallback to the last
    /// successfully built skeleton so that a somewhat reasonable experience can be maintained.  If we failed to do this
    /// and instead returned nothing, a user would find that practically all semantic experiences that depended on
    /// that particular project would fail or be seriously degraded (e.g. diagnostics).  To that end, it's better to
    /// limp along with stale date, then barrel on ahead with no data.
    /// <para/>
    /// The implementation works by keeping a mapping from <see cref="ProjectId"/> to the the metadata references
    /// for that project.  As long as the <see cref="Project.GetDependentSemanticVersionAsync"/> for that project
    /// is the same, then all the references of it can be reused.  When the compilation tracker forks itself, it 
    /// will also fork this, allow previously computed references to be used by later forks.  However, this means
    /// that later forks (esp. ones that fail to produce a skeleton, or which produce a skeleton for different
    /// semantics) will not leak backward to a prior compilation tracker point, causing it to see a view of the world
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

        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// The version of the project that these <see cref="_skeletonReferenceSet"/> correspond to.
        /// </summary>
        private VersionStamp? _version;

        /// <summary>
        /// Mapping from metadata-reference-properties to the actual metadata reference for them.
        /// </summary>
        private SkeletonReferenceSet? _skeletonReferenceSet;

        public static readonly CachedSkeletonReferences Empty =
            new(version: null, skeletonReferenceSet: null);

        private CachedSkeletonReferences(
            VersionStamp? version,
            SkeletonReferenceSet? skeletonReferenceSet)
        {
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
                return new CachedSkeletonReferences(_version, _skeletonReferenceSet);
            }
        }

        public async Task<MetadataReference> GetOrBuildReferenceAsync(
            SolutionState solution,
            ProjectReference projectReference,
            Compilation finalCompilation,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            // First see if we already have a cached reference for either finalCompilation or for projectReference.
            // If we have one for the latter, we'll make sure that it's version matches what we're asking for before
            // returning it.
            solution.Workspace.LogTestMessage($"Looking to see if we already have a skeleton assembly for {projectReference.ProjectId} before we build one...");
            var reference = await TryGetReferenceAsync(solution, projectReference, finalCompilation, version, cancellationToken).ConfigureAwait(false);
            if (reference != null)
            {
                solution.Workspace.LogTestMessage($"A reference was found {projectReference.ProjectId} so we're skipping the build.");
                return reference;
            }

            // okay, we don't have one. so create one now.

            // first, prepare image
            // * NOTE * image is cancellable, do not create it inside of conditional weak table.
            var service = solution.Workspace.Services.GetService<ITemporaryStorageService>();
            var image = MetadataOnlyImage.Create(solution.Workspace, service, finalCompilation, cancellationToken);

            if (image.IsEmpty)
            {
                // unfortunately, we couldn't create one. see if we have one from previous compilation., it might be
                // out-of-date big time, but better than nothing.
                reference = await TryGetReferenceAsync(solution, projectReference, finalCompilation, version: null, cancellationToken).ConfigureAwait(false);
                if (reference != null)
                {
                    solution.Workspace.LogTestMessage($"We failed to create metadata so we're using the one we just found from an earlier version.");
                    return reference;
                }
            }

            var tempReferenceSet = new SkeletonReferenceSet(image);
            var referenceSet = s_compilationToReferenceMap.GetValue(finalCompilation, _ => tempReferenceSet);
            if (tempReferenceSet != referenceSet)
            {
                // someone else has beaten us. 
                // let image go eagerly. otherwise, finalizer in temporary storage will take care of it
                image.Cleanup();
            }

            await (_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _version = version;
                _skeletonReferenceSet = referenceSet;
            }

            return await referenceSet.GetMetadataReferenceAsync(
                finalCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to get the <see cref="MetadataReference"/> associated with the provided <paramref name="projectReference"/>
        /// that produced the <see cref="Compilation"/> <paramref name="finalOrDeclarationCompilation"/>.
        /// </summary>
        public Task<MetadataReference?> TryGetReferenceAsync(
            SolutionState solution,
            ProjectReference projectReference,
            Compilation finalOrDeclarationCompilation,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            return TryGetReferenceAsync(solution, projectReference, finalOrDeclarationCompilation, (VersionStamp?)version, cancellationToken);
        }

        /// <remarks>
        /// <inheritdoc cref="TryGetReferenceAsync(SolutionState, ProjectReference, Compilation, VersionStamp, CancellationToken)"/>.
        /// If <paramref name="version"/> is <see langword="null"/>, any <see cref="MetadataReference"/> for that
        /// project may be returned, even if it doesn't correspond to that compilation.  This is useful in error tolerance
        /// cases as building a skeleton assembly may easily fail.  In that case it's better to use the last successfully
        /// built skeleton than just have no semantic information for that project at all.
        /// </remarks> 
        private async Task<MetadataReference?> TryGetReferenceAsync(
            SolutionState solution,
            ProjectReference projectReference,
            Compilation finalOrDeclarationCompilation,
            VersionStamp? version,
            CancellationToken cancellationToken)
        {
            // if we have one from snapshot cache, use it. it will make sure same compilation will get same metadata reference always.
            if (s_compilationToReferenceMap.TryGetValue(finalOrDeclarationCompilation, out var referenceSet))
            {
                solution.Workspace.LogTestMessage($"Found already cached metadata in {nameof(s_compilationToReferenceMap)} for the exact compilation");
                return await referenceSet.GetMetadataReferenceAsync(
                    finalOrDeclarationCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken).ConfigureAwait(false);
            }

            // okay, now use version based cache that can live multiple compilation as long as there is no semantic changes.

            var result = await TryGetReferenceAsync(projectReference, finalOrDeclarationCompilation, version, cancellationToken).ConfigureAwait(false);
            if (result != null)
                solution.Workspace.LogTestMessage($"Found already cached metadata for the branch and version {version}");

            return result;
        }

        private async Task<MetadataReference?> TryGetReferenceAsync(
            ProjectReference projectReference,
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

            return await set.GetMetadataReferenceAsync(
                finalOrDeclarationCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken).ConfigureAwait(false);
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
                Compilation compilation, ImmutableArray<string> aliases, bool embedInteropTypes, CancellationToken cancellationToken)
            {
                var key = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);

                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!_metadataReferences.TryGetValue(key, out var weakMetadata) ||
                        !weakMetadata.TryGetTarget(out var metadataReference))
                    {
                        // here we give out strong reference to compilation. so there is possibility that we end up making 2 compilations for same project alive.
                        // one for final compilation and one for declaration only compilation. but the final compilation will be eventually kicked out from compilation cache
                        // if there is no activity on the project. or the declaration compilation will go away if the project that depends on the reference doesn't have any
                        // activity when it is kicked out from compilation cache. if there is an activity, then both will updated as activity happens.
                        // so simply put, things will go away when compilations are kicked out from the cache or due to user activity.
                        //
                        // there is one case where we could have 2 compilations for same project alive. if a user opens a file that requires a skeleton assembly when the skeleton
                        // assembly project didn't reach the final stage yet and then the user opens another document that is part of the skeleton assembly project 
                        // and then never change it. declaration compilation will be alive by skeleton assembly and final compilation will be alive by background compiler.
                        metadataReference = _image.CreateReference(aliases, embedInteropTypes, new DeferredDocumentationProvider(compilation));
                        weakMetadata = new WeakReference<MetadataReference>(metadataReference);
                        _metadataReferences[key] = weakMetadata;
                    }

                    return metadataReference;
                }
            }
        }
    }
}
