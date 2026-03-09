// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.VisualStudio.LanguageServices.TaskList;

[ExportWorkspaceServiceFactory(typeof(VisualStudioDiagnosticIdCache)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioDiagnosticIdCacheFactory(
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new VisualStudioDiagnosticIdCache(
            workspaceServices.Workspace,
            threadingContext,
            listenerProvider);
    }
}

internal class VisualStudioDiagnosticIdCache : IWorkspaceService
{
    // This dictionary maps ProjectIds to a descriptor list.
    private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>> _projectIdToDiagnosticIdsCache = [];
    private readonly AsyncBatchingWorkQueue<ProjectId> _projectDescriptorRefreshQueue;

    private readonly Workspace _workspace;
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly IAsynchronousOperationListener _listener;

    public VisualStudioDiagnosticIdCache(
        Workspace workspace,
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _workspace = workspace;
        _analyzerService = workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);

        _projectDescriptorRefreshQueue = new AsyncBatchingWorkQueue<ProjectId>(
            delay: DelayTimeSpan.Short,
            processBatchAsync: RefreshCachedDiagnosticIdsAsync,
            equalityComparer: EqualityComparer<ProjectId>.Default,
            asyncListener: _listener,
            cancellationToken: threadingContext.DisposalToken);

        _workspace.RegisterWorkspaceChangedHandler(WorkspaceChanged);

        foreach (var projectId in _workspace.CurrentSolution.ProjectIds)
        {
            _projectDescriptorRefreshQueue.AddWork(projectId);
        }
    }

    public bool TryGetDiagnosticIds(ProjectId projectId, [NotNullWhen(returnValue: true)] out ImmutableHashSet<string>? diagnosticIds)
        => _projectIdToDiagnosticIdsCache.TryGetValue(projectId, out diagnosticIds);

    private void WorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        var workspaceChanges = e.NewSolution.GetChanges(e.OldSolution);

        foreach (var addedProject in workspaceChanges.GetAddedProjects())
        {
            _projectDescriptorRefreshQueue.AddWork(addedProject.Id);
        }

        foreach (var removedProject in workspaceChanges.GetRemovedProjects())
        {
            _projectDescriptorRefreshQueue.AddWork(removedProject.Id);
        }

        foreach (var projectChange in workspaceChanges.GetProjectChanges())
        {
            var oldProject = projectChange.OldProject;
            var newProject = projectChange.NewProject;

            var analyzersChanged = !oldProject.AnalyzerReferences.Equals(newProject.AnalyzerReferences);
            if (analyzersChanged)
            {
                _projectDescriptorRefreshQueue.AddWork(projectChange.NewProject.Id);
            }
        }
    }

    private async ValueTask RefreshCachedDiagnosticIdsAsync(
        ImmutableSegmentedList<ProjectId> projectIds,
        CancellationToken cancellationToken)
    {
        foreach (var projectId in projectIds)
        {
            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                _projectIdToDiagnosticIdsCache.TryRemove(projectId, out _);
                continue;
            }

            var descriptorMap = await _analyzerService.GetDiagnosticDescriptorsPerReferenceAsync(
                project.Solution,
                project.Id,
                cancellationToken).ConfigureAwait(false);

            _projectIdToDiagnosticIdsCache[project.Id] = [.. descriptorMap.Values.SelectMany(static descriptors => descriptors.Select(descriptor => descriptor.Id))];
        }
    }
}
