// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class ProjectDependencyService
    {
        private const string PersistenceName = "<GRAPH>";

        private static readonly ConditionalWeakTable<Workspace, WorkspaceTracker> workspaceTrackers =
            new ConditionalWeakTable<Workspace, WorkspaceTracker>();

        public static async Task<ProjectDependencyGraph> GetDependencyGraphAsync(Solution solution, CancellationToken cancellationToken)
        {
            var tracker = TrackWorkspace(solution.Workspace);
            var oldGraph = await tracker.GetGraphAsync(cancellationToken).ConfigureAwait(false);
            return ProjectDependencyGraph.From(solution, oldGraph, cancellationToken);
        }

        private static WorkspaceTracker TrackWorkspace(Workspace workspace)
        {
            // only add if the workspace is not currently being tracked.
            return workspaceTrackers.GetValue(workspace, ws => new WorkspaceTracker(workspace));
        }

        private static void StopTracking(Workspace workspace)
        {
            workspaceTrackers.Remove(workspace);
        }

        /// <summary>
        /// The workspace tracker tracks workspaces changes and resets the lazily created graph if
        /// project references change
        /// </summary>
        private class WorkspaceTracker
        {
            private readonly WeakReference<Workspace> weakWorkspace;
            private readonly NonReentrantLock lazyGate = new NonReentrantLock();

            private SolutionId solutionId;
            private AsyncLazy<ProjectDependencyGraph> lazyGraph;

            internal WorkspaceTracker(Workspace workspace)
            {
                this.weakWorkspace = new WeakReference<Workspace>(workspace);
                var solutionToLoad = workspace.CurrentSolution;
                this.solutionId = solutionToLoad.Id;
                this.lazyGraph = new AsyncLazy<ProjectDependencyGraph>(c => LoadOrComputeGraphAsync(solutionToLoad, c), cacheResult: true);

                workspace.WorkspaceChanged += this.OnWorkspaceChanged;
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                if (args.Kind == WorkspaceChangeKind.SolutionAdded)
                {
                    // no lock needed for solutionId. it will be only mutated in this
                    // method and it is already serialized.
                    this.solutionId = args.NewSolution.Id;
                    this.RecomputeGraphLazily(args.NewSolution);
                    return;
                }

                if (args.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    if (args.OldSolution.Id == this.solutionId)
                    {
                        this.StopTracking();
                    }

                    return;
                }

                if (args.NewSolution.Id == this.solutionId)
                {
                    switch (args.Kind)
                    {
                        case WorkspaceChangeKind.SolutionCleared:
                            this.RecomputeGraphLazily(args.NewSolution);
                            break;

                        case WorkspaceChangeKind.SolutionChanged:
                        case WorkspaceChangeKind.SolutionReloaded:
                        case WorkspaceChangeKind.ProjectChanged:
                        case WorkspaceChangeKind.ProjectAdded:
                        case WorkspaceChangeKind.ProjectRemoved:
                        case WorkspaceChangeKind.ProjectReloaded:
                            this.RecomputeLazilyOrUpdateGraph(args.NewSolution, args.ProjectId);
                            break;

                        default:
                            this.RecomputeLazilyOrMoveToLatestSolution(args.NewSolution, args.ProjectId);
                            break;
                    }
                }
            }

            public void StopTracking()
            {
                // time to save?
                var lazyGraph = this.GetLazyGraph(CancellationToken.None);
                ProjectDependencyGraph graph;
                if (lazyGraph.TryGetValue(out graph))
                {
                    this.SaveGraphAsync(graph, CancellationToken.None).Wait();
                }

                Workspace workspace;
                if (this.weakWorkspace.TryGetTarget(out workspace))
                {
                    workspace.WorkspaceChanged -= this.OnWorkspaceChanged;
                    ProjectDependencyService.StopTracking(workspace);
                }
            }

            public Task<ProjectDependencyGraph> GetGraphAsync(CancellationToken cancellationToken)
            {
                return this.GetLazyGraph(cancellationToken).GetValueAsync(cancellationToken);
            }

            private AsyncLazy<ProjectDependencyGraph> GetLazyGraph(CancellationToken cancellationToken)
            {
                using (this.lazyGate.DisposableWait(cancellationToken))
                {
                    return this.lazyGraph;
                }
            }

            private void SetLazyGraph(AsyncLazy<ProjectDependencyGraph> lazyGraph)
            {
                using (this.lazyGate.DisposableWait(CancellationToken.None))
                {
                    this.lazyGraph = lazyGraph;
                }
            }

            private async Task<ProjectDependencyGraph> LoadOrComputeGraphAsync(Solution solution, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(solution.BranchId == solution.Workspace.PrimaryBranchId);

                // TODO: make this async too
                var persistenceService = WorkspaceService.GetService<IPersistentStorageService>(solution.Workspace);

                ProjectDependencyGraph graph;
                using (var storage = persistenceService.GetStorage(solution))
                using (var stream = await storage.ReadStreamAsync(PersistenceName, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        using (var reader = new ObjectReader(stream))
                        {
                            graph = ProjectDependencyGraph.From(solution, reader, cancellationToken);
                            if (graph != null)
                            {
                                return graph;
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // do it the hard way!
                graph = ProjectDependencyGraph.From(solution, cancellationToken);

                // since we built it, we may as well save it for next time
                await SaveGraphAsync(graph, cancellationToken).ConfigureAwait(false);

                return graph;
            }

            private async Task SaveGraphAsync(ProjectDependencyGraph graph, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(graph.Solution.BranchId == graph.Solution.Workspace.PrimaryBranchId);

                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream))
                {
                    graph.WriteTo(writer);
                    stream.Position = 0;

                    var persistenceService = WorkspaceService.GetService<IPersistentStorageService>(graph.Solution.Workspace);
                    using (var storage = persistenceService.GetStorage(graph.Solution))
                    {
                        await storage.WriteStreamAsync(PersistenceName, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public void RecomputeGraphLazily(Solution solution)
            {
                SetLazyGraph(new AsyncLazy<ProjectDependencyGraph>(c => Task.FromResult(ProjectDependencyGraph.From(solution, c)), cacheResult: true));
            }

            private static readonly Func<Solution, ProjectDependencyGraph, CancellationToken, Task<ProjectDependencyGraph>> updateReferences =
                (s, g, c) => Task.FromResult(ProjectDependencyGraph.From(s, g, c));

            private void RecomputeLazilyOrUpdateGraph(Solution solution, ProjectId projectID)
            {
                RecomputeLazilyOrMakeGraphUpToDate(solution, projectID, updateReferences);
            }

            private static readonly Func<Solution, ProjectDependencyGraph, CancellationToken, Task<ProjectDependencyGraph>> withNewSolution =
                (s, g, c) => Task.FromResult(g.WithNewSolution(s));

            private void RecomputeLazilyOrMoveToLatestSolution(Solution solution, ProjectId projectID)
            {
                RecomputeLazilyOrMakeGraphUpToDate(solution, projectID, withNewSolution);
            }

            private void RecomputeLazilyOrMakeGraphUpToDate(Solution solution, ProjectId projectID, Func<Solution, ProjectDependencyGraph, CancellationToken, Task<ProjectDependencyGraph>> newGraphGetter)
            {
                var lazyGraph = this.GetLazyGraph(CancellationToken.None);

                ProjectDependencyGraph graph;
                if (!lazyGraph.TryGetValue(out graph) ||
                    (projectID != null && !graph.Solution.ProjectIds.Contains(projectID)))
                {
                    RecomputeGraphLazily(solution);
                    return;
                }

                this.SetLazyGraph(new AsyncLazy<ProjectDependencyGraph>(c => newGraphGetter(solution, graph, c), cacheResult: true));
            }
        }
    }
}