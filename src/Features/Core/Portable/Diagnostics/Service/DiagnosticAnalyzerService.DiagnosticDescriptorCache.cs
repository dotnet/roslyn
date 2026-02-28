// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    internal class DiagnosticDescriptorCache
    {
        private readonly ConcurrentDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> _projectIdToDescriptorCache = [];
        private readonly AsyncBatchingWorkQueue<ProjectId> _projectDescriptorRefreshQueue;

        private readonly Workspace _workspace;
        private readonly DiagnosticAnalyzerService _analyzerService;
        private readonly IAsynchronousOperationListener _listener;

        public DiagnosticDescriptorCache(Workspace workspace, DiagnosticAnalyzerService analyzerService, IAsynchronousOperationListener listener)
        {
            _workspace = workspace;
            _analyzerService = analyzerService;
            _listener = listener;

            _projectDescriptorRefreshQueue = new AsyncBatchingWorkQueue<ProjectId>(
                delay: DelayTimeSpan.Short,
                processBatchAsync: RefreshCachedDiagnosticDescriptorsAsync,
                equalityComparer: EqualityComparer<ProjectId>.Default,
                asyncListener: _listener,
                cancellationToken: CancellationToken.None);

            _workspace.RegisterWorkspaceChangedHandler(WorkspaceChanged);

            foreach (var projectId in _workspace.CurrentSolution.ProjectIds)
            {
                _projectDescriptorRefreshQueue.AddWork(projectId);
            }
        }

        public bool TryGetDiagnosticDescriptorsPerReference(ProjectId projectId, [NotNullWhen(returnValue: true)] out ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>? descriptorsPerReference)
            => _projectIdToDescriptorCache.TryGetValue(projectId, out descriptorsPerReference);

        private void WorkspaceChanged(WorkspaceChangeEventArgs e)
        {
            var workspaceChanges = e.NewSolution.GetChanges(e.OldSolution);

            foreach (var newProject in workspaceChanges.GetAddedProjects())
            {
                _projectDescriptorRefreshQueue.AddWork(newProject.Id);
            }

            foreach (var removedProject in workspaceChanges.GetRemovedProjects())
            {
                _projectIdToDescriptorCache.TryRemove(removedProject.Id, out _);
            }

            foreach (var projectChange in workspaceChanges.GetProjectChanges())
            {
                var analyzersChanged = projectChange.GetAddedAnalyzerReferences().Any()
                    || projectChange.GetRemovedAnalyzerReferences().Any();

                if (analyzersChanged)
                {
                    _projectDescriptorRefreshQueue.AddWork(projectChange.NewProject.Id);
                }
            }
        }

        private async ValueTask RefreshCachedDiagnosticDescriptorsAsync(
            ImmutableSegmentedList<ProjectId> projectIds,
            CancellationToken cancellationToken)
        {
            foreach (var projectId in projectIds)
            {
                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null)
                    continue;

                var descriptorsPerReference = await _analyzerService.GetDiagnosticDescriptorsPerReferenceAsync(
                    project.Solution, project.Id, cancellationToken).ConfigureAwait(false);

                _projectIdToDescriptorCache[project.Id] = descriptorsPerReference;
            }
        }
    }
}
