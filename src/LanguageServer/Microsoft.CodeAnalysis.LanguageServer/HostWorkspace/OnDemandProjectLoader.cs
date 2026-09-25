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
    ILoggerFactory loggerFactory) : IOnDemandProjectLoader
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();

    private readonly object _activeLoadsGate = new();

    /// <summary>
    /// Coalesces demands from the same directory for the lifetime of discovery and the complete project-reference
    /// traversal. Individual project loads remain owned by <see cref="LanguageServerProjectLoader"/>.
    /// </summary>
    private readonly Dictionary<string, Task<bool>> _activeTraversals = new(PathUtilities.Comparer);

    private bool IsOnDemandLoadingEnabled
        => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand) &&
           !globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures);

    public Task<bool> StartLoadingAsync(DocumentUri uri)
    {
        if (!IsOnDemandLoadingEnabled || uri.ParsedDocumentUri?.IsFile != true)
        {
            return Task.FromResult(false);
        }

        var filePath = Path.GetFullPath(uri.GetDocumentFilePathFromUri());
        var directory = Path.GetDirectoryName(filePath);
        Contract.ThrowIfNull(directory);
        lock (_activeLoadsGate)
        {
            if (_activeTraversals.TryGetValue(directory, out var activeLoad))
                return activeLoad;

            var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
            var loadTask = Task.Run(() => LoadDiscoveredProjectsAsync(filePath, directory, workspaceFolders));
            _activeTraversals.Add(directory, loadTask);
            return loadTask;
        }
    }

    public ValueTask<Task> CaptureWorkspaceLoadSnapshotAsync()
    {
        if (!IsOnDemandLoadingEnabled)
            return new(Task.CompletedTask);

        lock (_activeLoadsGate)
        {
            return new(Task.WhenAll(_activeTraversals.Values));
        }
    }

    private async Task<bool> LoadDiscoveredProjectsAsync(string filePath, string directory, ImmutableHashSet<string> workspaceFolders)
    {
        using var _ = listener.BeginAsyncOperation(nameof(LoadDiscoveredProjectsAsync));

        try
        {
            _logger.LogDebug("Discovering a project on demand for '{DocumentPath}'.", filePath);
            var projectPaths = discovery.DiscoverProjects(filePath, workspaceFolders);
            if (projectPaths.IsEmpty)
                return false;

            var pendingProjects = new Queue<string>(projectPaths);
            var visitedProjects = new HashSet<string>(PathUtilities.Comparer);

            while (pendingProjects.Count > 0)
            {
                var projectsToLoad = new List<Task<LoadedProject>>();
                while (pendingProjects.TryDequeue(out var projectPath))
                {
                    projectPath = Path.GetFullPath(projectPath);
                    if (!visitedProjects.Add(projectPath))
                        continue;

                    _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);
                    projectsToLoad.Add(projectLoader.BeginLoadingProjectAsync(projectPath, isHighPriority: true));
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

            return true;
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand.");
            return true;
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
