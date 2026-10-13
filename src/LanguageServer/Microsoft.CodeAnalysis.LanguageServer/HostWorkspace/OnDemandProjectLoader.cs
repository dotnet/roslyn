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
            lspServices.GetRequiredService<LanguageServerWorkspaceFactory>(),
            projectSystem,
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            loggerFactory);
    }
}

internal sealed partial class OnDemandProjectLoader(
    OnDemandProjectLoader.ProjectDiscovery discovery,
    IWorkspaceFolderTracker workspaceFolderTracker,
    LanguageServerWorkspaceFactory workspaceFactory,
    LanguageServerProjectLoader projectLoader,
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListener listener,
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();

    private readonly object _activeLoadsGate = new();

    /// <summary>
    /// Maps each starting directory to the in-progress task that discovers and loads its projects and
    /// transitive project references. Requests from the same directory share the task.
    /// </summary>
    private readonly Dictionary<string, Task<Solution?>> _activeTraversals = new(PathUtilities.Comparer);

    private bool IsOnDemandLoadingEnabled
        => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) &&
           !globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures);

    public bool CanLoad(DocumentUri uri)
        => IsOnDemandLoadingEnabled && uri.ParsedDocumentUri?.IsFile == true;

    public ValueTask<Solution?> TryLoadProjectsAsync(DocumentUri uri)
    {
        if (!CanLoad(uri))
        {
            return new ValueTask<Solution?>((Solution?)null);
        }

        var filePath = Path.GetFullPath(uri.GetDocumentFilePathFromUri());
        var directory = Path.GetDirectoryName(filePath);
        Contract.ThrowIfNull(directory);
        lock (_activeLoadsGate)
        {
            if (_activeTraversals.TryGetValue(directory, out var activeLoad))
                return new(activeLoad);

            var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
            var loadTask = Task.Run(() => LoadDiscoveredProjectsAsync(directory, workspaceFolders));
            _activeTraversals.Add(directory, loadTask);
            return new(loadTask);
        }
    }

    public ValueTask<Solution> WaitForActiveLoadsAsync()
    {
        if (!IsOnDemandLoadingEnabled)
            return new(workspaceFactory.HostWorkspace.CurrentSolution);

        lock (_activeLoadsGate)
        {
            return _activeTraversals.Count == 0
                ? new(workspaceFactory.HostWorkspace.CurrentSolution)
                : new(WaitForLoadsAsync(Task.WhenAll(_activeTraversals.Values)));
        }

        async Task<Solution> WaitForLoadsAsync(Task loads)
        {
            await loads.ConfigureAwait(false);
            return workspaceFactory.HostWorkspace.CurrentSolution;
        }
    }

    private async Task<Solution?> LoadDiscoveredProjectsAsync(string directory, ImmutableHashSet<string> workspaceFolders)
    {
        using var _ = listener.BeginAsyncOperation(nameof(LoadDiscoveredProjectsAsync));

        try
        {
            _logger.LogDebug("Discovering projects on demand from '{Directory}'.", directory);
            var projectPaths = discovery.DiscoverProjects(directory, workspaceFolders);
            if (projectPaths.IsEmpty)
                return null;

            var pendingProjects = new Queue<string>(projectPaths);
            var visitedProjects = new HashSet<string>(PathUtilities.Comparer);

            while (pendingProjects.Count > 0)
            {
                var projectsToLoad = new List<Task<LoadedProject>>();
                while (pendingProjects.TryDequeue(out var projectPath))
                {
                    if (!visitedProjects.Add(projectPath))
                        continue;

                    _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);
                    projectsToLoad.Add(projectLoader.BeginLoadingProjectAsync(projectPath, LanguageServerProjectLoader.ProjectReloadPriority.High));
                }

                foreach (var project in await Task.WhenAll(projectsToLoad))
                {
                    if (!await project.WaitForLoadAsync(CancellationToken.None))
                        continue;

                    foreach (var referencePath in await project.GetProjectReferencePathsAsync())
                    {
                        if (discovery.IsSupportedProject(referencePath))
                            pendingProjects.Enqueue(referencePath);
                    }
                }
            }

            return workspaceFactory.HostWorkspace.CurrentSolution;
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
            return null;
        }
        finally
        {
            lock (_activeLoadsGate)
            {
                _activeTraversals.Remove(directory);
            }
        }
    }
}
