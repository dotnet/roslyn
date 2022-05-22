// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class BackgroundCompiler : IDisposable
    {
        private Workspace _workspace;
        private readonly TaskQueue _taskQueue;

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used to keep a strong reference to the built compilations so they are not GC'd")]
        private Compilation?[]? _mostRecentCompilations;

        private readonly object _buildGate = new();
        private CancellationTokenSource _cancellationSource = new();

        public BackgroundCompiler(Workspace workspace)
        {
            _workspace = workspace;

            // make a scheduler that runs on the thread pool
            var listenerProvider = workspace.Services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>();
            _taskQueue = new TaskQueue(listenerProvider.GetListener(), TaskScheduler.Default);

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _workspace.DocumentOpened += OnDocumentOpened;
            _workspace.DocumentClosed += OnDocumentClosed;
        }

        public void Dispose()
        {
            if (_workspace != null)
            {
                CancelBuild(releasePreviousCompilations: true);

                _workspace.DocumentClosed -= OnDocumentClosed;
                _workspace.DocumentOpened -= OnDocumentOpened;
                _workspace.WorkspaceChanged -= OnWorkspaceChanged;

                _workspace = null!;
            }
        }

        private void OnDocumentOpened(object? sender, DocumentEventArgs args)
            => Rebuild(args.Document.Project.Solution, args.Document.Project.Id);

        private void OnDocumentClosed(object? sender, DocumentEventArgs args)
            => Rebuild(args.Document.Project.Solution, args.Document.Project.Id);

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionRemoved:
                    CancelBuild(releasePreviousCompilations: true);
                    break;

                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.ProjectRemoved:
                    if (args.NewSolution.ProjectIds.Count == 0)
                    {
                        // Close solution no longer triggers a SolutionRemoved event,
                        // so we need to make an explicitly check for ProjectRemoved event.
                        CancelBuild(releasePreviousCompilations: true);
                    }
                    else
                    {
                        Rebuild(args.NewSolution);
                    }

                    break;

                default:
                    Rebuild(args.NewSolution, args.ProjectId);
                    break;
            }
        }

        private void Rebuild(Solution solution, ProjectId? initialProject = null)
        {
            lock (_buildGate)
            {
                // Keep the previous compilations around so that we can incrementally
                // build the current compilations without rebuilding the entire DeclarationTable
                CancelBuild(releasePreviousCompilations: false);

                var allOpenProjects = _workspace.GetOpenDocumentIds().Select(d => d.ProjectId).ToSet();

                // don't even get started if there is nothing to do
                if (allOpenProjects.Count > 0)
                {
                    _ = BuildCompilationsAsync(solution, initialProject, allOpenProjects);
                }
            }
        }

        private void CancelBuild(bool releasePreviousCompilations)
        {
            lock (_buildGate)
            {
                _cancellationSource.Cancel();
                _cancellationSource = new CancellationTokenSource();
                if (releasePreviousCompilations)
                {
                    _mostRecentCompilations = null;
                }
            }
        }

        private Task BuildCompilationsAsync(
            Solution solution,
            ProjectId? initialProject,
            ISet<ProjectId> allOpenProjects)
        {
            var cancellationToken = _cancellationSource.Token;
            return _taskQueue.ScheduleTask(
                "BackgroundCompiler.BuildCompilationsAsync",
                () => BuildCompilationsAsync(solution, initialProject, allOpenProjects, cancellationToken),
                cancellationToken);
        }

        private Task BuildCompilationsAsync(
            Solution solution,
            ProjectId? initialProject,
            ISet<ProjectId> projectsToBuild,
            CancellationToken cancellationToken)
        {
            var allProjectIds = new List<ProjectId>();
            if (initialProject != null)
            {
                allProjectIds.Add(initialProject);
            }

            allProjectIds.AddRange(projectsToBuild.Where(p => p != initialProject));

            var logger = Logger.LogBlock(FunctionId.BackgroundCompiler_BuildCompilationsAsync, cancellationToken);

            var compilationTasks = allProjectIds
                .Select(solution.GetProject)
                .Select(async p =>
                {
                    if (p is null)
                        return null;

                    return await p.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                })
                .ToArray();
            return Task.WhenAll(compilationTasks).SafeContinueWith(t =>
                {
                    logger.Dispose();
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        lock (_buildGate)
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                _mostRecentCompilations = t.Result;
                            }
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }
    }
}
