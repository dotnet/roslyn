// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OnDemandProjectLoader)), Shared]
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
    LanguageServerProjectLoader projectSystem,
    LanguageServerWorkspaceFactory workspaceFactory,
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListener listener,
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader, IOnServerShutdown, System.IAsyncDisposable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    private readonly CancellationTokenSource _shutdownSource = new();

    /// <summary>
    /// Protects <see cref="_activeLoads"/>, its entries' project-path sets, and <see cref="_shutdownTask"/>.
    /// </summary>
    private readonly object _activeLoadsGate = new();

    /// <summary>
    /// Coalesces demands from the same directory for the lifetime of discovery and the complete project-reference
    /// traversal. Each project-path set is also the traversal's visited set, allowing documents in other directories
    /// to join an operation that already includes their project. Individual project loads remain owned by
    /// <see cref="LanguageServerProjectLoader"/>.
    /// </summary>
    private readonly Dictionary<string, (Task Task, HashSet<string> ProjectPaths)> _activeLoads = new(PathUtilities.Comparer);
    private Task? _shutdownTask;

    public Task StartLoadingAsync(DocumentUri uri)
    {
        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) ||
            globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures) ||
            uri.ParsedDocumentUri?.IsFile != true)
        {
            return Task.CompletedTask;
        }

        var filePath = Path.GetFullPath(uri.GetDocumentFilePathFromUri());
        var directory = Path.GetDirectoryName(filePath);
        Contract.ThrowIfNull(directory);
        lock (_activeLoadsGate)
        {
            if (_shutdownTask is not null)
                return Task.CompletedTask;

            if (_activeLoads.TryGetValue(directory, out var activeLoad))
                return activeLoad.Task;

            var solution = workspaceFactory.HostWorkspace.CurrentSolution;
            var documentIds = solution.GetDocumentIdsWithFilePath(filePath);
            if (!documentIds.IsEmpty)
            {
                return Task.WhenAll(_activeLoads.Values
                    .Where(load => documentIds.Any(id =>
                        solution.GetRequiredProject(id.ProjectId).FilePath is { } path &&
                        load.ProjectPaths.Contains(Path.GetFullPath(path))))
                    .Select(load => load.Task));
            }

            var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
            var shutdownToken = _shutdownSource.Token;
            var projectPaths = new HashSet<string>(PathUtilities.Comparer);
            var loadTask = Task.Run(() => LoadDiscoveredProjectsAsync(filePath, directory, workspaceFolders, projectPaths, shutdownToken));
            _activeLoads.Add(directory, (loadTask, projectPaths));
            return loadTask;
        }
    }

    public async ValueTask<ProjectLoadSnapshot> CaptureWorkspaceLoadSnapshotAsync()
    {
        Task[] activeLoads;
        CancellationToken shutdownToken;
        lock (_activeLoadsGate)
        {
            activeLoads = [.. _activeLoads.Values.Select(load => load.Task)];
            if (_shutdownTask is not null)
                return new(Task.WhenAll(activeLoads));

            shutdownToken = _shutdownSource.Token;
        }

        var projectLoads = await projectSystem.CaptureProjectLoadSnapshotAsync(shutdownToken);
        // Active operations include discovery and children not yet registered with the project system.
        return new(Task.WhenAll([projectLoads.Completion, .. activeLoads]));
    }

    private async Task LoadDiscoveredProjectsAsync(
        string filePath,
        string directory,
        ImmutableHashSet<string> workspaceFolders,
        HashSet<string> visitedProjectPaths,
        CancellationToken shutdownToken)
    {
        using var _ = listener.BeginAsyncOperation(nameof(LoadDiscoveredProjectsAsync));

        try
        {
            _logger.LogDebug("Discovering a project on demand for '{DocumentPath}'.", filePath);
            var projectPaths = discovery.DiscoverProjects(filePath, workspaceFolders, shutdownToken);
            await LoadProjectClosureAsync(projectPaths, visitedProjectPaths, shutdownToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
        }
        finally
        {
            lock (_activeLoadsGate)
                _activeLoads.Remove(directory);
        }
    }

    private async Task LoadProjectClosureAsync(
        ImmutableArray<string> rootProjectPaths, HashSet<string> visitedProjectPaths, CancellationToken cancellationToken)
    {
        var pendingLoads = new List<Task<(LoadedProject project, bool loadedSuccessfully)>>();

        var traversalTask = TraverseAsync();
        // Observe traversal and every started child together, even if traversal exits early.
        // WhenAll preserves unexpected failures when other children are canceled during shutdown.
        await traversalTask.NoThrowAwaitable(captureContext: false);
        await Task.WhenAll([traversalTask, .. pendingLoads]).ConfigureAwait(false);

        async Task TraverseAsync()
        {
            foreach (var projectPath in rootProjectPaths)
                QueueProject(projectPath);

            while (pendingLoads.Count > 0)
            {
                var completedLoad = await Task.WhenAny(pendingLoads).ConfigureAwait(false);
                pendingLoads.Remove(completedLoad);

                var (project, loadedSuccessfully) = await completedLoad.ConfigureAwait(false);
                if (!loadedSuccessfully)
                    continue;

                foreach (var referencePath in await project.GetProjectReferencePathsAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (discovery.IsSupportedProject(referencePath))
                        QueueProject(referencePath);
                }
            }
        }

        void QueueProject(string projectPath)
        {
            cancellationToken.ThrowIfCancellationRequested();

            projectPath = Path.GetFullPath(projectPath);
            // StartLoadingAsync also reads this visited set when matching a document to an active traversal.
            lock (_activeLoadsGate)
            {
                if (!visitedProjectPaths.Add(projectPath))
                    return;
            }

            _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);
            pendingLoads.Add(LoadProjectAsync(projectPath));
        }

        async Task<(LoadedProject project, bool loadedSuccessfully)> LoadProjectAsync(string projectPath)
        {
            var project = await projectSystem.BeginLoadingProjectAsync(
                projectPath, LanguageServerProjectLoader.ProjectReloadPriority.High, cancellationToken).ConfigureAwait(false);
            var loadedSuccessfully = await project.WaitForLoadAsync(cancellationToken).ConfigureAwait(false);
            return (project, loadedSuccessfully);
        }
    }

    public Task ShutdownAsync()
    {
        lock (_activeLoadsGate)
        {
            if (_shutdownTask is not null)
                return _shutdownTask;

            var activeLoads = Task.WhenAll(_activeLoads.Values.Select(load => load.Task));
            // Cancel outside the gate, and make concurrent shutdown/disposal callers await cancellation too.
            return _shutdownTask = Task.Run(async () =>
            {
                try
                {
                    _shutdownSource.Cancel();
                }
                finally
                {
                    await activeLoads.ConfigureAwait(false);
                }
            });
        }
    }

    public Task ExitAsync()
        => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync().ConfigureAwait(false);
        _shutdownSource.Dispose();
    }
}
