// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

internal sealed partial class SymbolTreeInfoCacheServiceFactory
{
    internal sealed partial class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService, IDisposable
    {
        /// <summary>
        /// Same value as SolutionCrawlerTimeSpan.EntireProjectWorkerBackOff
        /// </summary>
        private static readonly TimeSpan EntireProjectWorkerBackOff = TimeSpan.FromMilliseconds(5000);

        private static readonly TaskScheduler s_exclusiveScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

        private readonly ConcurrentDictionary<ProjectId, (VersionStamp semanticVersion, SymbolTreeInfo info)> _projectIdToInfo = new();
        private readonly ConcurrentDictionary<PortableExecutableReference, MetadataInfo> _peReferenceToInfo = new();

        private readonly CancellationTokenSource _tokenSource = new();

        private readonly Workspace _workspace;
        private readonly AsyncBatchingWorkQueue<ProjectId> _workQueue;

        /// <summary>
        /// Scheduler to run our tasks on.  If we're in the remote host , we'll run all our tasks concurrently.
        /// Otherwise, we will run them serially using <see cref="s_exclusiveScheduler"/>
        /// </summary>
        private readonly TaskScheduler _scheduler;

        public SymbolTreeInfoCacheService(Workspace workspace, IAsynchronousOperationListener listener)
        {
            _workspace = workspace;
            _workQueue = new AsyncBatchingWorkQueue<ProjectId>(
                EntireProjectWorkerBackOff,
                ProcessProjectsAsync,
                EqualityComparer<ProjectId>.Default,
                listener,
                _tokenSource.Token);

            _scheduler = workspace.Kind == WorkspaceKind.RemoteWorkspace ? TaskScheduler.Default : s_exclusiveScheduler;
        }

        void IDisposable.Dispose()
            => _tokenSource.Cancel();

        private Task CreateWorkAsync(Func<Task> createWorkAsync, CancellationToken cancellationToken)
            => Task.Factory.StartNew(createWorkAsync, cancellationToken, TaskCreationOptions.None, _scheduler).Unwrap();

        /// <summary>
        /// Gets the latest computed <see cref="SymbolTreeInfo"/> for the requested <paramref name="reference"/>.
        /// This may return an index corresponding to a prior version of the reference if it has since changed.
        /// Another system is responsible for bringing these indices up to date in the background.
        /// </summary>
        public async ValueTask<SymbolTreeInfo?> TryGetPotentiallyStaleMetadataSymbolTreeInfoAsync(
            Project project,
            PortableExecutableReference reference,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return null;

            // Kick off the work to update the data we have for this project.
            _workQueue.AddWork(project.Id);

            // See if the last value produced exactly matches what the caller is asking for.  If so, return that.
            if (_peReferenceToInfo.TryGetValue(reference, out var metadataInfo))
                return metadataInfo.SymbolTreeInfo;

            // If we didn't have it in our cache, see if we can load it from disk.
            var solution = project.Solution;
            var info = await SymbolTreeInfo.LoadAnyInfoForMetadataReferenceAsync(solution, reference, cancellationToken).ConfigureAwait(false);
            if (info is null)
                return null;

            var referencingProjects = new HashSet<ProjectId>(solution.Projects.Where(p => p.MetadataReferences.Contains(reference)).Select(p => p.Id));

            // attempt to add this item to the map.  But defer to whatever is in the map now if something else beat us to this.
            return _peReferenceToInfo.GetOrAdd(reference, new MetadataInfo(info, referencingProjects)).SymbolTreeInfo;
        }

        public async ValueTask<SymbolTreeInfo?> TryGetPotentiallyStaleSourceSymbolTreeInfoAsync(
            Project project, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return null;

            // Kick off the work to update the data we have for this project.
            _workQueue.AddWork(project.Id);

            // See if the last value produced exactly matches what the caller is asking for.  If so, return that.
            if (_projectIdToInfo.TryGetValue(project.Id, out var projectInfo))
                return projectInfo.info;

            // If we didn't have it in our cache, see if we can load some version of it from disk.
            var info = await SymbolTreeInfo.LoadAnyInfoForSourceAssemblyAsync(project, cancellationToken).ConfigureAwait(false);
            if (info is null)
                return null;

            // attempt to add this item to the map.  But defer to whatever is in the map now if something else beat
            // us to this.  Don't provide a version here so that the next time we update this data it will get
            // overwritten with the latest computed data.
            return _projectIdToInfo.GetOrAdd(project.Id, (semanticVersion: default, info)).info;
        }

        private async ValueTask ProcessProjectsAsync(
            ImmutableSegmentedList<ProjectId> projectIds, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;

            foreach (var projectId in projectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = solution.GetProject(projectId);
                if (project is not { SupportsCompilation: true })
                    continue;

                // NOTE: currently we process projects serially.  This is because the same metadata reference might be
                // found in multiple projects and we can't currently process that in parallel.
                await AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
            }

            // Now that we've produced all the indices for the projects asked for, also remove any indices for projects
            // no longer in the solution.
            var removedProjectIds = _projectIdToInfo.Keys.Except(solution.ProjectIds).ToArray();
            foreach (var projectId in removedProjectIds)
                this.RemoveProject(projectId);
        }

        private async Task AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            // We can compute source-symbols in parallel with the metadata symbols.
            tasks.Add(CreateWorkAsync(() => this.UpdateSourceSymbolTreeInfoAsync(project, cancellationToken), cancellationToken));

            // Add tasks to update the symboltree for all metadata references.  As these are all distinct, they can run
            // in parallel as we won't be trying to update the associated data for the same reference at the same time.
            foreach (var reference in project.MetadataReferences.OfType<PortableExecutableReference>().Distinct())
                tasks.Add(CreateWorkAsync(() => UpdateReferenceAsync(project, reference, cancellationToken), cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task UpdateSourceSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
        {
            // Find the top-level-semantic-version of this project.  We only want to recompute if it has changed. This is
            // because the symboltree contains the names of the types/namespaces in the project and would not change
            // if the semantic-version of the project hasn't changed.  We also do not need to check the 'dependent
            // version'.  As this is just tracking parent/child relationships of namespace/type names for the source
            // types in this project, this cannot change based on what happens in other projects.
            var semanticVersion = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            if (!_projectIdToInfo.TryGetValue(project.Id, out var projectInfo) ||
                projectInfo.semanticVersion != semanticVersion)
            {
                // If the checksum is the same (which can happen if we loaded the previous index from disk), then no
                // need to recompute.
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
                if (projectInfo.info?.Checksum != checksum)
                {
                    // Otherwise, looks like things changed.  Compute and persist the latest index.
                    var info = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(
                        project, checksum, cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfNull(info);
                    Contract.ThrowIfTrue(info.Checksum != checksum, "If we computed a SymbolTreeInfo, then its checksum must match our checksum.");

                    // Mark that we're up to date with this project.  Future calls with the same semantic-version or
                    // checksum can bail out immediately.
                    _projectIdToInfo[project.Id] = (semanticVersion, info);
                }
            }
        }

        private async Task UpdateReferenceAsync(
            Project project,
            PortableExecutableReference reference,
            CancellationToken cancellationToken)
        {
            var checksum = SymbolTreeInfo.GetMetadataChecksum(project.Solution.Services, reference, cancellationToken);
            if (!_peReferenceToInfo.TryGetValue(reference, out var metadataInfo) ||
                metadataInfo.SymbolTreeInfo.Checksum != checksum)
            {
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    project.Solution, reference, checksum, cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfNull(info);
                Contract.ThrowIfTrue(info.Checksum != checksum, "If we computed a SymbolTreeInfo, then its checksum must match our checksum.");

                metadataInfo = new MetadataInfo(info, metadataInfo.ReferencingProjects ?? new HashSet<ProjectId>());
                _peReferenceToInfo[reference] = metadataInfo;
            }

            // Keep track that this dll is referenced by this project.
            lock (metadataInfo.ReferencingProjects)
            {
                metadataInfo.ReferencingProjects.Add(project.Id);
            }
        }

        public void RemoveProject(ProjectId projectId)
        {
            _projectIdToInfo.TryRemove(projectId, out _);
            RemoveMetadataReferences(projectId);
        }

        private void RemoveMetadataReferences(ProjectId projectId)
        {
            foreach (var (reference, info) in _peReferenceToInfo.ToArray())
            {
                lock (info.ReferencingProjects)
                {
                    info.ReferencingProjects.Remove(projectId);

                    // If this metadata dll isn't referenced by any project.  We can just dump it.
                    if (info.ReferencingProjects.Count == 0)
                        _peReferenceToInfo.TryRemove(reference, out _);
                }
            }
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public struct TestAccessor(SymbolTreeInfoCacheService service)
        {
            public readonly Task AnalyzeSolutionAsync()
            {
                foreach (var projectId in service._workspace.CurrentSolution.ProjectIds)
                    service._workQueue.AddWork(projectId);

                return service._workQueue.WaitUntilCurrentBatchCompletesAsync();
            }
        }
    }
}
