// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        public bool TryGetStateChecksums([NotNullWhen(true)] out SolutionStateChecksums? stateChecksums)
            => _lazyChecksums.TryGetValue(out stateChecksums);

        public bool TryGetStateChecksums(ProjectId projectId, [NotNullWhen(true)] out SolutionStateChecksums? stateChecksums)
        {
            AsyncLazy<SolutionStateChecksums>? checksums;
            lock (_lazyProjectChecksums)
            {
                if (!_lazyProjectChecksums.TryGetValue(projectId, out checksums) ||
                    checksums == null)
                {
                    stateChecksums = null;
                    return false;
                }
            }

            return checksums.TryGetValue(out stateChecksums);
        }

        public Task<SolutionStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
            => _lazyChecksums.GetValueAsync(cancellationToken);

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
        public async Task<SolutionStateChecksums> GetStateChecksumsAsync(
            ProjectId projectId,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(projectId);

            AsyncLazy<SolutionStateChecksums>? checksums;
            lock (_lazyProjectChecksums)
            {
                if (!_lazyProjectChecksums.TryGetValue(projectId, out checksums))
                {
                    checksums = Compute(projectId);
                    _lazyProjectChecksums.Add(projectId, checksums);
                }
            }

            var collection = await checksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection;

            // Extracted as a local function to prevent delegate allocations when not needed.
            AsyncLazy<SolutionStateChecksums> Compute(ProjectId projectId)
            {
                var projectsToInclude = new HashSet<ProjectId>();
                AddReferencedProjects(projectsToInclude, projectId);

                return AsyncLazy.Create(c => ComputeChecksumsAsync(projectsToInclude, c));
            }

            void AddReferencedProjects(HashSet<ProjectId> result, ProjectId projectId)
            {
                if (!result.Add(projectId))
                    return;

                var projectState = this.GetProjectState(projectId);
                if (projectState == null)
                    return;

                foreach (var refProject in projectState.ProjectReferences)
                {
                    // Note: it's possible in the workspace to see project-ids that don't have a corresponding project
                    // state.  While not desirable, we allow project's to have refs to projects that no longer exist
                    // anymore.  This state is expected to be temporary until the project is explicitly told by the
                    // host to remove the reference.  We do not expose this through the full Solution/Project which
                    // filters out this case already (in Project.ProjectReferences). However, becausde we're at the
                    // ProjectState level it cannot do that filtering unless examined through us (the SolutionState).
                    if (this.ProjectStates.ContainsKey(refProject.ProjectId))
                        AddReferencedProjects(result, refProject.ProjectId);
                }
            }
        }

        /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
        public async Task<Checksum> GetChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            var checksums = await GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
            return checksums.Checksum;
        }

        /// <param name="projectsToInclude">Cone of projects to compute a checksum for.  Pass in <see langword="null"/>
        /// to get a checksum for the entire solution</param>
        private async Task<SolutionStateChecksums> ComputeChecksumsAsync(
            HashSet<ProjectId>? projectsToInclude,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.SolutionState_ComputeChecksumsAsync, FilePath, cancellationToken))
                {
                    // get states by id order to have deterministic checksum.  Limit expensive computation to the
                    // requested set of projects if applicable.
                    var orderedProjectIds = ChecksumCache.GetOrCreate(ProjectIds, _ => ProjectIds.OrderBy(id => id.Id).ToImmutableArray());
                    var projectChecksumTasks = orderedProjectIds
                        .Select(id => (state: ProjectStates[id], mustCompute: projectsToInclude == null || projectsToInclude.Contains(id)))
                        .Where(t => RemoteSupportedLanguages.IsSupported(t.state.Language))
                        .Select(async t =>
                        {
                            // if it's a project that's specifically in the sync'ed cone, include this checksum so that
                            // this project definitely syncs over.
                            if (t.mustCompute)
                                return (t.state.Id, await t.state.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false));

                            // If it's a project that is not in the cone, still try to get the latest checksum for it if
                            // we have it.  That way we don't send over a checksum *without* that project, causing the
                            // OOP side to throw that project away (along with all the compilation info stored with it).
                            if (t.state.TryGetStateChecksums(out var stateChecksums))
                                return (t.state.Id, stateChecksums);

                            // We have never computed the checksum for this project.  Don't send anything for it.
                            return default((ProjectId, ProjectStateChecksums));
                        })
                        .ToArray();

                    var serializer = Services.GetRequiredService<ISerializerService>();
                    var attributesChecksum = this.SolutionAttributes.Checksum;

                    var frozenSourceGeneratedDocumentIdentityChecksum = Checksum.Null;
                    var frozenSourceGeneratedDocumentTextChecksum = Checksum.Null;

                    if (FrozenSourceGeneratedDocumentState != null)
                    {
                        frozenSourceGeneratedDocumentIdentityChecksum = serializer.CreateChecksum(FrozenSourceGeneratedDocumentState.Identity, cancellationToken);
                        frozenSourceGeneratedDocumentTextChecksum = (await FrozenSourceGeneratedDocumentState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false)).Text;
                    }

                    var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(AnalyzerReferences,
                        _ => new ChecksumCollection(AnalyzerReferences.SelectAsArray(r => serializer.CreateChecksum(r, cancellationToken))));

                    var tuples = await Task.WhenAll(projectChecksumTasks).ConfigureAwait(false);
                    using var _1 = ArrayBuilder<ProjectId>.GetInstance(projectChecksumTasks.Length, out var projectIds);
                    using var _2 = ArrayBuilder<Checksum>.GetInstance(projectChecksumTasks.Length, out var projectChecksums);
                    foreach (var (projectId, projectStateChecksums) in tuples)
                    {
                        if (projectStateChecksums != null)
                        {
                            projectIds.Add(projectId);
                            projectChecksums.Add(projectStateChecksums.Checksum);
                        }
                    }

                    return new SolutionStateChecksums(
                        attributesChecksum,
                        projectIds.ToImmutableAndClear(),
                        new ChecksumCollection(projectChecksums.ToImmutableAndClear()),
                        analyzerReferenceChecksums,
                        frozenSourceGeneratedDocumentIdentityChecksum,
                        frozenSourceGeneratedDocumentTextChecksum);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
