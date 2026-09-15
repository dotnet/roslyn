// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
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
    {
        var projectSystem = lspServices.GetRequiredService<LanguageServerProjectSystem>();
        var loggerFactory = lspServices.GetRequiredService<ILoggerFactory>();

        return new OnDemandProjectLoader(
            new OnDemandProjectLoader.ProjectDiscovery(
                projectSystem.GetSupportedProjectFileExtensions(),
                loggerFactory),
            lspServices.GetRequiredService<IWorkspaceFolderTracker>(),
            projectSystem,
            lspServices.GetRequiredService<LanguageServerWorkspaceFactory>(),
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            loggerFactory);
    }
}

internal sealed partial class OnDemandProjectLoader(
    OnDemandProjectLoader.ProjectDiscovery discovery,
    IWorkspaceFolderTracker workspaceFolderTracker,
    LanguageServerProjectSystem projectSystem,
    LanguageServerWorkspaceFactory workspaceFactory,
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListener listener,
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader, IDisposable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    private readonly CancellationTokenSource _shutdownSource = new();
    private readonly object _activeLoadsGate = new();
    private readonly HashSet<Task> _activeLoads = [];

    public Task StartLoadingAsync(DocumentUri uri)
    {
        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) ||
            globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures) ||
            uri.ParsedDocumentUri?.IsFile != true)
        {
            return Task.CompletedTask;
        }

        var filePath = uri.GetDocumentFilePathFromUri();
        if (!workspaceFactory.HostWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).IsEmpty)
            return Task.CompletedTask;

        var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
        var shutdownToken = _shutdownSource.Token;
        _logger.LogDebug("Discovering a project on demand for '{DocumentPath}'.", filePath);
        var discoveryTask = Task.Run(
            () => discovery.DiscoverProjects(filePath, workspaceFolders, shutdownToken),
            shutdownToken);
        return TrackLoadAsync(LoadDiscoveredProjectsAsync(discoveryTask, shutdownToken));
    }

    public async ValueTask<Task> GetWorkspaceLoadTaskAsync()
    {
        var shutdownToken = _shutdownSource.Token;
        Task[] activeLoads;
        lock (_activeLoadsGate)
            activeLoads = [.. _activeLoads];

        var projectLoads = await projectSystem.GetWaitForAllProjectLoadsTaskAsync(shutdownToken);
        return Task.WhenAll([projectLoads, .. activeLoads]);
    }

    private Task TrackLoadAsync(Task loadTask)
    {
        lock (_activeLoadsGate)
            _activeLoads.Add(loadTask);

        _ = loadTask.ContinueWith(
            static (completedLoad, state) => ((OnDemandProjectLoader)state!).RemoveActiveLoad(completedLoad),
            this,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return loadTask;
    }

    private void RemoveActiveLoad(Task loadTask)
    {
        lock (_activeLoadsGate)
            _activeLoads.Remove(loadTask);
    }

    private async Task LoadDiscoveredProjectsAsync(
        Task<ImmutableArray<string>> discoveryTask,
        CancellationToken shutdownToken)
    {
        using var _ = listener.BeginAsyncOperation(nameof(LoadDiscoveredProjectsAsync));

        try
        {
            var projectPaths = await discoveryTask.ConfigureAwait(false);
            if (projectPaths.IsEmpty)
                return;

            foreach (var projectPath in projectPaths)
                _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);

            await LoadProjectClosureAsync(projectPaths, shutdownToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
        }
    }

    private async Task LoadProjectClosureAsync(
        ImmutableArray<string> rootProjectPaths, CancellationToken cancellationToken)
    {
        var visitedProjectPaths = new HashSet<string>(PathUtilities.Comparer);
        var pendingLoads = new List<Task<(LoadedProject project, bool loadedSuccessfully)>>();

        foreach (var projectPath in rootProjectPaths)
            QueueProject(projectPath);

        while (pendingLoads.Count > 0)
        {
            var completedLoad = await Task.WhenAny(pendingLoads).ConfigureAwait(false);
            pendingLoads.Remove(completedLoad);

            var (project, loadedSuccessfully) = await completedLoad.ConfigureAwait(false);
            if (!loadedSuccessfully)
                continue;

            foreach (var referencePath in await projectSystem.GetSupportedProjectReferencesAsync(project))
                QueueProject(referencePath);
        }

        return;

        void QueueProject(string projectPath)
        {
            cancellationToken.ThrowIfCancellationRequested();

            projectPath = Path.GetFullPath(projectPath);
            if (visitedProjectPaths.Add(projectPath))
                pendingLoads.Add(LoadProjectAsync(projectPath));
        }

        async Task<(LoadedProject project, bool loadedSuccessfully)> LoadProjectAsync(string projectPath)
        {
            var project = await projectSystem.BeginLoadingProjectAsync(projectPath).ConfigureAwait(false);
            var loadedSuccessfully = await project.WaitForLoadAsync(cancellationToken).ConfigureAwait(false);
            return (project, loadedSuccessfully);
        }
    }

    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(OnDemandProjectLoader loader)
    {
        public void TrackLoad(Task loadTask)
            => loader.TrackLoadAsync(loadTask);

        public Task LoadProjectClosureAsync(
            ImmutableArray<string> rootProjectPaths, CancellationToken cancellationToken)
            => loader.LoadProjectClosureAsync(rootProjectPaths, cancellationToken);
    }
}
