// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal class BackgroundCompiler : IDisposable
    {
        private Workspace _workspace;
        private readonly IWorkspaceTaskScheduler _compilationScheduler;

        // Used to keep a strong reference to the built compilations so they are not GC'd
        private Compilation[] _mostRecentCompilations;

        private readonly object _buildGate = new object();
        private CancellationTokenSource _cancellationSource;

        public BackgroundCompiler(Workspace workspace)
        {
            _workspace = workspace;

            // make a scheduler that runs on the thread pool
            var taskSchedulerFactory = workspace.Services.GetService<IWorkspaceTaskSchedulerFactory>();
            _compilationScheduler = taskSchedulerFactory.CreateBackgroundTaskScheduler();

            _cancellationSource = new CancellationTokenSource();
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

                _workspace = null;
            }
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs args)
        {
            Rebuild(args.Document.Project.Solution, args.Document.Project.Id);
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs args)
        {
            Rebuild(args.Document.Project.Solution, args.Document.Project.Id);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
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

        private void Rebuild(Solution solution, ProjectId initialProject = null)
        {
            lock (_buildGate)
            {
                // Keep the previous compilations around so that we can incrementally
                // build the current compilations without rebuilding the entire DeclarationTable
                CancelBuild(releasePreviousCompilations: false);

                if (ServiceFeatureOnOffOptions.IsPowerSaveModeEnabled(solution.Options))
                {
                    return;
                }

                var allProjects = _workspace.GetOpenDocumentIds().Select(d => d.ProjectId).ToSet();

                // don't even get started if there is nothing to do
                if (allProjects.Count > 0)
                {
                    BuildCompilationsAsync(solution, initialProject, allProjects);
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

        private void BuildCompilationsAsync(
            Solution solution,
            ProjectId initialProject,
            ISet<ProjectId> allProjects)
        {
            var cancellationToken = _cancellationSource.Token;
            _compilationScheduler.ScheduleTask(
                () => BuildCompilationsAsync(solution, initialProject, allProjects, cancellationToken),
                "BackgroundCompiler.BuildCompilationsAsync",
                cancellationToken);
        }

        private Task BuildCompilationsAsync(
            Solution solution,
            ProjectId initialProject,
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

            var compilationTasks = allProjectIds.Select(solution.GetProject).Where(p => p != null).Select(p => p.GetCompilationAsync(cancellationToken)).ToArray();
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
