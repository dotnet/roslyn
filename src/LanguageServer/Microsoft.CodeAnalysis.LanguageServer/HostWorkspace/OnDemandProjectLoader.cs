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
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
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
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            loggerFactory);
    }
}

internal sealed partial class OnDemandProjectLoader(
    OnDemandProjectLoader.ProjectDiscovery discovery,
    IWorkspaceFolderTracker workspaceFolderTracker,
    LanguageServerProjectLoader projectLoader,
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListener listener,
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader, IOnServerShutdown
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    private readonly CancellationTokenSource _shutdownCancellationSource = new();

    private readonly object _activeLoadsGate = new();

    /// <summary>
    /// Maps each starting directory to the in-progress task that discovers and loads its projects and
    /// transitive project references. Requests from the same directory share the task.
    /// </summary>
    private readonly Dictionary<string, Task> _activeTraversals = new(PathUtilities.Comparer);

    private bool IsOnDemandLoadingEnabled
        => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) &&
           !globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures);

    public bool CanLoad(DocumentUri uri)
        => IsOnDemandLoadingEnabled &&
           uri.ParsedDocumentUri?.IsFile == true;

    public Task TryLoadProjectsAsync(DocumentUri uri)
    {
        if (!CanLoad(uri) || _shutdownCancellationSource.IsCancellationRequested)
            return Task.CompletedTask;

        var filePath = Path.GetFullPath(uri.GetDocumentFilePathFromUri());
        var directory = Path.GetDirectoryName(filePath);
        Contract.ThrowIfNull(directory);
        lock (_activeLoadsGate)
        {
            if (_activeTraversals.TryGetValue(directory, out var activeLoad))
                return activeLoad;

            var asyncToken = listener.BeginAsyncOperation(nameof(TryLoadProjectsAsync));

            var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
            var cancellationToken = _shutdownCancellationSource.Token;
            // Do not pass the cancellation token to Task.Run. LoadDiscoveredProjectsAsync handles cancellation
            // and uses its finally block to remove the traversal and dispose asyncToken.
            var loadTask = Task.Run(() => LoadDiscoveredProjectsAsync(directory, workspaceFolders, asyncToken, cancellationToken));
            _activeTraversals.Add(directory, loadTask);
            return loadTask;
        }
    }

    public Task WaitForActiveLoadsAsync()
    {
        if (!IsOnDemandLoadingEnabled)
            return Task.CompletedTask;

        lock (_activeLoadsGate)
        {
            return _activeTraversals.Count == 0
                ? Task.CompletedTask
                : Task.WhenAll(_activeTraversals.Values);
        }
    }

    public Task ShutdownAsync()
    {
        _shutdownCancellationSource.Cancel();
        _shutdownCancellationSource.Dispose();
        return Task.CompletedTask;
    }

    public Task ExitAsync()
        => Task.CompletedTask;

    private async Task LoadDiscoveredProjectsAsync(
        string directory,
        ImmutableHashSet<string> workspaceFolders,
        IAsyncToken asyncToken,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Discovering projects on demand from '{Directory}'.", directory);
            var projectPaths = discovery.DiscoverProjects(directory, workspaceFolders, cancellationToken);
            if (projectPaths.IsEmpty)
                return;

            var projectsToLoad = new List<Task<LoadedProject?>>();
            var visitedProjects = new HashSet<string>(PathUtilities.Comparer);

            foreach (var projectPath in projectPaths)
                QueueProject(projectPath, projectsToLoad, visitedProjects);

            while (projectsToLoad.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var completedLoad = await Task.WhenAny(projectsToLoad).WaitAsync(cancellationToken);
                projectsToLoad.Remove(completedLoad);

                var project = await completedLoad;
                if (project is null)
                    continue;

                foreach (var referencePath in await project.GetProjectReferencePathsAsync(cancellationToken))
                {
                    if (discovery.IsSupportedProject(referencePath))
                        QueueProject(referencePath, projectsToLoad, visitedProjects);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
        }
        finally
        {
            lock (_activeLoadsGate)
            {
                _activeTraversals.Remove(directory);
            }
            asyncToken.Dispose();
        }

        void QueueProject(string projectPath, List<Task<LoadedProject?>> projectsToLoad, HashSet<string> visitedProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (visitedProjects.Add(projectPath))
                projectsToLoad.Add(LoadProjectAsync(projectPath, cancellationToken));
        }

        async Task<LoadedProject?> LoadProjectAsync(string projectPath, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);
                var project = await projectLoader.BeginLoadingProjectAsync(
                    projectPath, LanguageServerProjectLoader.ProjectReloadPriority.High).WaitAsync(cancellationToken);

                return await project.WaitForLoadAsync(cancellationToken) ? project : null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception exception) when (FatalError.ReportAndCatch(exception))
            {
                _logger.LogError(exception, "Failed to load project on demand for '{ProjectPath}'.", projectPath);
                return null;
            }
        }
    }
}
