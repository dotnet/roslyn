// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private static readonly ConditionalWeakTable<Compilation, MetadataOnlyReferenceSet> s_compilationToReferenceMap = new();

        private readonly object _gate = new();

        /// <summary>
        /// Mapping from <see cref="ProjectId"/> to the skeleton <see cref="MetadataReference"/>s for it.
        /// </summary>
        private ImmutableDictionary<ProjectId, MetadataOnlyReferenceSet> _projectIdToReferenceMap;

        public static readonly CachedSkeletonReferences Empty = new(ImmutableDictionary<ProjectId, MetadataOnlyReferenceSet>.Empty);

        private CachedSkeletonReferences(
            ImmutableDictionary<ProjectId, MetadataOnlyReferenceSet> projectIdToReferenceMap)
        {
            _projectIdToReferenceMap = projectIdToReferenceMap;
        }

        public CachedSkeletonReferences Clone()
            => new(_projectIdToReferenceMap);

        internal MetadataReference GetOrBuildReference(
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
            if (TryGetReference(
                    solution, projectReference, finalCompilation, version, cancellationToken, out var reference))
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
                if (TryGetReference(
                        solution, projectReference, finalCompilation, version: null, cancellationToken, out reference))
                {
                    solution.Workspace.LogTestMessage($"We failed to create metadata so we're using the one we just found from an earlier version.");
                    return reference;
                }
            }

            var newReferenceSet = new MetadataOnlyReferenceSet(version, image);
            var referenceSet = s_compilationToReferenceMap.GetValue(finalCompilation, _ => newReferenceSet);
            if (newReferenceSet != referenceSet)
            {
                // someone else has beaten us. 
                // let image go eagerly. otherwise, finalizer in temporary storage will take care of it
                image.Cleanup();

                // return new reference
                return referenceSet.GetMetadataReference(
                    finalCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken);
            }
            else
            {
                solution.Workspace.LogTestMessage($"Successfully stored the metadata generated for {projectReference.ProjectId}");
            }

            // record it to version based cache as well. snapshot cache always has a higher priority. we don't need to check returned set here
            // since snapshot based cache will take care of same compilation for us.

            lock (_gate)
            {
                _projectIdToReferenceMap = _projectIdToReferenceMap.Remove(projectReference.ProjectId);
                _projectIdToReferenceMap = _projectIdToReferenceMap.Add(projectReference.ProjectId, referenceSet);
            }

            // return new reference
            return referenceSet.GetMetadataReference(
                finalCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken);
        }

        /// <summary>
        /// Tries to get the <see cref="MetadataReference"/> associated with the provided <paramref name="projectReference"/>
        /// that produced the <see cref="Compilation"/> <paramref name="finalOrDeclarationCompilation"/>.
        /// </summary>
        internal bool TryGetReference(
            SolutionState solution,
            ProjectReference projectReference,
            Compilation finalOrDeclarationCompilation,
            VersionStamp version,
            CancellationToken cancellationToken,
            out MetadataReference reference)
        {
            return TryGetReference(solution, projectReference, finalOrDeclarationCompilation, (VersionStamp?)version, cancellationToken, out reference);
        }

        /// <remarks>
        /// <inheritdoc cref="TryGetReference(SolutionState, ProjectReference, Compilation, VersionStamp, CancellationToken, out MetadataReference)"/>.
        /// If <paramref name="version"/> is <see langword="null"/>, any <see cref="MetadataReference"/> for that
        /// project may be returned, even if it doesn't correspond to that compilation.  This is useful in error tolerance
        /// cases as building a skeleton assembly may easily fail.  In that case it's better to use the last successfully
        /// built skeleton than just have no semantic information for that project at all.
        /// </remarks> 
        private bool TryGetReference(
            SolutionState solution,
            ProjectReference projectReference,
            Compilation finalOrDeclarationCompilation,
            VersionStamp? version,
            CancellationToken cancellationToken,
            out MetadataReference reference)
        {
            // if we have one from snapshot cache, use it. it will make sure same compilation will get same metadata reference always.
            if (s_compilationToReferenceMap.TryGetValue(finalOrDeclarationCompilation, out var referenceSet))
            {
                solution.Workspace.LogTestMessage($"Found already cached metadata in {nameof(s_compilationToReferenceMap)} for the exact compilation");
                reference = referenceSet.GetMetadataReference(
                    finalOrDeclarationCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken);
                return true;
            }

            // okay, now use version based cache that can live multiple compilation as long as there is no semantic changes.

            if (TryGetReference(
                    projectReference, finalOrDeclarationCompilation, version, cancellationToken, out reference))
            {
                solution.Workspace.LogTestMessage($"Found already cached metadata for the branch and version {version}");
                return true;
            }

            // noop, we don't have any
            reference = null;
            return false;
        }

        private bool TryGetReference(
            ProjectReference projectReference,
            Compilation finalOrDeclarationCompilation,
            VersionStamp? version,
            CancellationToken cancellationToken,
            out MetadataReference reference)
        {
            if (_projectIdToReferenceMap.TryGetValue(projectReference.ProjectId, out var referenceSet) &&
               (version == null || referenceSet.Version == version))
            {
                // record it to snapshot based cache.
                var newReferenceSet = s_compilationToReferenceMap.GetValue(finalOrDeclarationCompilation, _ => referenceSet);
                reference = newReferenceSet.GetMetadataReference(
                    finalOrDeclarationCompilation, projectReference.Aliases, projectReference.EmbedInteropTypes, cancellationToken);
                return true;
            }

            reference = null;
            return false;
        }

        private class MetadataOnlyReferenceSet
        {
            // use WeakReference so we don't keep MetadataReference's alive if they are not being consumed
            private readonly NonReentrantLock _gate = new(useThisInstanceForSynchronization: true);

            private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _metadataReferences = new();
            private readonly MetadataOnlyImage _image;

            public readonly VersionStamp Version;

            public MetadataOnlyReferenceSet(VersionStamp version, MetadataOnlyImage image)
            {
                Version = version;
                _image = image;
            }

            public MetadataReference GetMetadataReference(
                Compilation compilation, ImmutableArray<string> aliases, bool embedInteropTypes, CancellationToken cancellationToken)
            {
                var key = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);

                using (_gate.DisposableWait(cancellationToken))
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
