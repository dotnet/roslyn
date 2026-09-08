// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(IOnDemandProjectLoader)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OnDemandProjectLoaderFactory(
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListenerProvider listenerProvider) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OnDemandProjectLoader(
            lspServices.GetRequiredService<WorkspaceProjectDiscoveryService>(),
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            lspServices.GetRequiredService<LanguageServerWorkspaceFactory>(),
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            lspServices.GetRequiredService<ILoggerFactory>());
}

internal sealed class OnDemandProjectLoader : IOnDemandProjectLoader, IDisposable
{
    private readonly WorkspaceProjectDiscoveryService _discoveryService;
    private readonly LanguageServerProjectSystem _projectSystem;
    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly IGlobalOptionService _globalOptionService;
    private readonly IAsynchronousOperationListener _listener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _shutdownSource = new();

    public OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        LanguageServerProjectSystem projectSystem,
        LanguageServerWorkspaceFactory workspaceFactory,
        IGlobalOptionService globalOptionService,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
    {
        _discoveryService = discoveryService;
        _projectSystem = projectSystem;
        _workspaceFactory = workspaceFactory;
        _globalOptionService = globalOptionService;
        _listener = listener;
        _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    }

    public Task StartLoadingAsync(DocumentUri uri, ImmutableHashSet<string> workspaceFolders)
    {
        if (!_globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) ||
            _globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures) ||
            uri.ParsedDocumentUri?.IsFile != true)
        {
            return Task.CompletedTask;
        }

        var filePath = uri.GetDocumentFilePathFromUri();
        if (!_workspaceFactory.HostWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).IsEmpty)
            return Task.CompletedTask;

        var discoveryTask = Task.Run(() => DiscoverProjects(filePath, workspaceFolders), _shutdownSource.Token);
        return LoadDiscoveredProjectsAsync(discoveryTask);
    }

    private async Task LoadDiscoveredProjectsAsync(Task<ImmutableArray<string>> discoveryTask)
    {
        using var token = _listener.BeginAsyncOperation(nameof(LoadDiscoveredProjectsAsync));
        try
        {
            var candidateProjects = await discoveryTask.ConfigureAwait(false);
            if (candidateProjects.IsEmpty)
                return;

            foreach (var projectPath in candidateProjects)
                _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);

            await LoadProjectClosureAsync(candidateProjects, _shutdownSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
        }
    }

    public Task WaitForWorkspaceLoadsAsync()
        => _projectSystem.WaitForAllProjectLoadsAsync(_shutdownSource.Token);

    private ImmutableArray<string> DiscoverProjects(string filePath, ImmutableHashSet<string> workspaceFolders)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = _discoveryService.DiscoverProjects(filePath, workspaceFolders, _shutdownSource.Token);
        _logger.LogDebug(
            "Discovered {ProjectCount} candidate project(s) for '{DocumentPath}' in {ElapsedMilliseconds} ms.",
            result.Length,
            filePath,
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private async Task LoadProjectClosureAsync(
        ImmutableArray<string> projectFilePaths,
        CancellationToken cancellationToken)
    {
        var pendingLoads = new List<Task<(LoadedProject project, bool loadedSuccessfully)>>();
        var visitedPaths = new HashSet<string>(PathUtilities.Comparer);

        foreach (var projectFilePath in projectFilePaths)
            QueueProject(projectFilePath);

        while (pendingLoads.Count > 0)
        {
            // Process loads as they complete, rather than in enqueue order, so a slow project doesn't hold up
            // ones that already finished from expanding the dependency closure.
            var completedTask = await Task.WhenAny(pendingLoads).WaitAsync(cancellationToken).ConfigureAwait(false);
            pendingLoads.Remove(completedTask);

            var (project, loadedSuccessfully) = await completedTask.ConfigureAwait(false);
            if (loadedSuccessfully)
            {
                foreach (var reference in await _projectSystem.GetProjectReferencesAsync(project).ConfigureAwait(false))
                    QueueProject(reference);
            }
        }

        void QueueProject(string projectFilePath)
        {
            projectFilePath = Path.GetFullPath(projectFilePath);
            if (visitedPaths.Add(projectFilePath))
                pendingLoads.Add(LoadProjectAsync(projectFilePath));
        }

        async Task<(LoadedProject project, bool loadedSuccessfully)> LoadProjectAsync(string projectFilePath)
        {
            var project = await _projectSystem.BeginLoadingProjectAsync(projectFilePath).ConfigureAwait(false);
            var loadedSuccessfully = await project.WaitForLoadAsync(cancellationToken).ConfigureAwait(false);
            return (project, loadedSuccessfully);
        }
    }

    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
    }

}
