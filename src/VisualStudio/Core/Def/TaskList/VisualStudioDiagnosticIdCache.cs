// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

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
    /// <summary>
    /// This dictionary maps ProjectIds to a set of DiagnosticIds.
    /// </summary>
    /// <remarks>
    /// A <see cref="ProjectId" /> being in the map means we are tracking changes for this project
    /// and will update diagnostic ids when AnalyzerReferences change. A null value
    /// means that we haven't computed the diagnostic ids for this project id yet.
    /// </remarks>
    private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>?> _projectIdToDiagnosticIdsCache = [];
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
    }

    /// <summary>
    /// We will only cache diagnostic ids for projects which have been registered by the <see cref="AbstractLegacyProject"/>.
    /// </summary>
    public void RegisterProject(ProjectId projectId)
    {
        // Ensure we have an entry for this projectId in case we get a workspace change event before
        // we set it in RefreshCacheDiagnosticIdsAsync.
        _projectIdToDiagnosticIdsCache.TryAdd(projectId, null);
        _projectDescriptorRefreshQueue.AddWork(projectId);
    }

    public bool TryGetDiagnosticIds(ProjectId projectId, [NotNullWhen(returnValue: true)] out ImmutableHashSet<string>? diagnosticIds)
        => _projectIdToDiagnosticIdsCache.TryGetValue(projectId, out diagnosticIds) && diagnosticIds != null;

    private void WorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        if (_projectIdToDiagnosticIdsCache.IsEmpty)
        {
            return;
        }

        var workspaceChanges = e.NewSolution.GetChanges(e.OldSolution);

        foreach (var removedProject in workspaceChanges.GetRemovedProjects())
        {
            if (_projectIdToDiagnosticIdsCache.ContainsKey(removedProject.Id))
            {
                _projectDescriptorRefreshQueue.AddWork(removedProject.Id);
            }
        }

        foreach (var projectChange in workspaceChanges.GetProjectChanges())
        {
            if (_projectIdToDiagnosticIdsCache.ContainsKey(projectChange.ProjectId))
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

            var list = await _analyzerService.GetAllDiagnosticIdsAsync(
                project.Solution,
                project.Id,
                cancellationToken).ConfigureAwait(false);

            _projectIdToDiagnosticIdsCache[project.Id] = list;
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(VisualStudioDiagnosticIdCache diagnosticCache)
    {
        public int RegisteredProjectCount => diagnosticCache._projectIdToDiagnosticIdsCache.Count;
    }
}
