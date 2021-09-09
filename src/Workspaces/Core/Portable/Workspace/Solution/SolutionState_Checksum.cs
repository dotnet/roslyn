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
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        public bool TryGetStateChecksums([NotNullWhen(true)] out SolutionStateChecksums? stateChecksums)
            => _lazyChecksums.TryGetValue(out stateChecksums);

        public bool TryGetStateChecksums(ProjectId projectId, out (SerializableOptionSet options, SolutionStateChecksums checksums) result)
        {
            (SerializableOptionSet options, ValueSource<SolutionStateChecksums> checksums) value;
            lock (_lazyProjectChecksums)
            {
                if (!_lazyProjectChecksums.TryGetValue(projectId, out value) ||
                    value.checksums == null)
                {
                    result = default;
                    return false;
                }
            }

            if (!value.checksums.TryGetValue(out var stateChecksums))
            {
                result = default;
                return false;
            }

            result = (value.options, stateChecksums);
            return true;
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

            (SerializableOptionSet options, ValueSource<SolutionStateChecksums> checksums) value;
            lock (_lazyProjectChecksums)
            {
                if (!_lazyProjectChecksums.TryGetValue(projectId, out value))
                {
                    value = Compute(projectId);
                    _lazyProjectChecksums.Add(projectId, value);
                }
            }

            var collection = await value.checksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection;

            // Extracted as a local function to prevent delegate allocations when not needed.
            (SerializableOptionSet, ValueSource<SolutionStateChecksums>) Compute(ProjectId projectId)
            {
                var projectsToInclude = new HashSet<ProjectId>();
                AddReferencedProjects(projectsToInclude, projectId);

                // we're syncing a subset of projects, so only sync the options for the particular languages
                // we're syncing over.
                var languages = projectsToInclude.Select(id => ProjectStates[id].Language)
                                                 .Where(s => RemoteSupportedLanguages.IsSupported(s))
                                                 .ToImmutableHashSet();

                var options = this.Options.WithLanguages(languages);

                return (options, new AsyncLazy<SolutionStateChecksums>(
                    c => ComputeChecksumsAsync(projectsToInclude, options, c), cacheResult: true));
            }

            void AddReferencedProjects(HashSet<ProjectId> result, ProjectId projectId)
            {
                if (!result.Add(projectId))
                    return;

                var projectState = this.GetProjectState(projectId);
                if (projectState == null)
                    return;

                foreach (var refProject in projectState.ProjectReferences)
                    AddReferencedProjects(result, refProject.ProjectId);
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
            SerializableOptionSet options,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.SolutionState_ComputeChecksumsAsync, FilePath, cancellationToken))
                {
                    // get states by id order to have deterministic checksum.  Limit to the requested set of projects
                    // if applicable.
                    var orderedProjectIds = ChecksumCache.GetOrCreate(ProjectIds, _ => ProjectIds.OrderBy(id => id.Id).ToImmutableArray());
                    var projectChecksumTasks = orderedProjectIds.Where(id => projectsToInclude == null || projectsToInclude.Contains(id))
                                                                .Select(id => ProjectStates[id])
                                                                .Where(s => RemoteSupportedLanguages.IsSupported(s.Language))
                                                                .Select(s => s.GetChecksumAsync(cancellationToken))
                                                                .ToArray();

                    var serializer = _solutionServices.Workspace.Services.GetRequiredService<ISerializerService>();
                    var attributesChecksum = serializer.CreateChecksum(SolutionAttributes, cancellationToken);

                    var optionsChecksum = serializer.CreateChecksum(options, cancellationToken);

                    var frozenSourceGeneratedDocumentIdentityChecksum = Checksum.Null;
                    var frozenSourceGeneratedDocumentTextChecksum = Checksum.Null;

                    if (FrozenSourceGeneratedDocumentState != null)
                    {
                        frozenSourceGeneratedDocumentIdentityChecksum = serializer.CreateChecksum(FrozenSourceGeneratedDocumentState.Identity, cancellationToken);
                        frozenSourceGeneratedDocumentTextChecksum = (await FrozenSourceGeneratedDocumentState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false)).Text;
                    }

                    var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(AnalyzerReferences,
                        _ => new ChecksumCollection(AnalyzerReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));

                    var projectChecksums = await Task.WhenAll(projectChecksumTasks).ConfigureAwait(false);
                    return new SolutionStateChecksums(attributesChecksum, optionsChecksum, new ChecksumCollection(projectChecksums), analyzerReferenceChecksums, frozenSourceGeneratedDocumentIdentityChecksum, frozenSourceGeneratedDocumentTextChecksum);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
