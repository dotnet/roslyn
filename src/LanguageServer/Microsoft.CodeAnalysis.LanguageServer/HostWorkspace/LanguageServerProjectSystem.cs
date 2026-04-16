// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem : LanguageServerProjectLoader
{
    private readonly ILogger _logger;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;
    private readonly ProjectSystemProjectFactory _hostProjectFactory;

    /// <summary>
    /// Guards access to <see cref="_currentWorkspaceFolderPaths"/>.
    /// </summary>
    private readonly object _folderPathsLock = new();

    /// <summary>
    /// The current set of active workspace folder paths, normalized to absolute paths.
    /// Updated by <see cref="SetInitialWorkspaceFolderPaths"/> and <see cref="OnWorkspaceFoldersChangedAsync"/>.
    /// </summary>
    private ImmutableArray<string> _currentWorkspaceFolderPaths = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerProjectSystem(
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
            : base(
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));
        _hostProjectFactory = workspaceFactory.HostProjectFactory;
        var workspace = workspaceFactory.HostWorkspace;
        _projectFileExtensionRegistry = new ProjectFileExtensionRegistry(new DiagnosticReporter(workspace));
    }

    public async Task OpenSolutionAsync(string solutionFilePath)
    {
        _logger.LogInformation(string.Format(LanguageServerResources.Loading_0, solutionFilePath));
        _hostProjectFactory.SolutionPath = solutionFilePath;

        var (_, projects) = await SolutionFileReader.ReadSolutionFileAsync(solutionFilePath, DiagnosticReportingMode.Throw, CancellationToken.None);
        foreach (var (path, guid) in projects)
        {
            await BeginLoadingProjectAsync(path, guid);
        }
        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
    }

    public async Task OpenProjectsAsync(ImmutableArray<string> projectFilePaths)
    {
        if (!projectFilePaths.Any())
            return;

        foreach (var path in projectFilePaths)
        {
            await BeginLoadingProjectAsync(path, projectGuid: null);
        }
        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
    }

    /// <summary>
    /// Records the initial set of workspace folder paths (typically supplied during LSP initialization).
    /// This is used to establish the initial scope for project-unload decisions.
    /// </summary>
    public void SetInitialWorkspaceFolderPaths(ImmutableArray<string> folderPaths)
    {
        lock (_folderPathsLock)
        {
            _currentWorkspaceFolderPaths = folderPaths;
        }

        _logger.LogInformation("Initial workspace folders set: {count} folder(s).", folderPaths.Length);
    }

    /// <summary>
    /// Applies a workspace-folder change (added and removed folders) and unloads any tracked projects
    /// that are no longer reachable from the updated set of active workspace folders.
    /// </summary>
    public async Task OnWorkspaceFoldersChangedAsync(
        ImmutableArray<string> addedFolderPaths,
        ImmutableArray<string> removedFolderPaths,
        CancellationToken cancellationToken)
    {
        ImmutableArray<string> newFolderPaths;
        lock (_folderPathsLock)
        {
            // Compute the new folder set: current + added - removed (using path-aware comparer).
            var updated = _currentWorkspaceFolderPaths
                .Where(p => !removedFolderPaths.Contains(p, PathUtilities.Comparer))
                .Concat(addedFolderPaths)
                .ToImmutableArray();

            _currentWorkspaceFolderPaths = updated;
            newFolderPaths = updated;
        }

        _logger.LogInformation(
            "Workspace folders changed. Added: {added}, Removed: {removed}. Active folders: {count}.",
            addedFolderPaths.Length, removedFolderPaths.Length, newFolderPaths.Length);

        await UnloadProjectsNotReachableFromWorkspaceFoldersAsync(newFolderPaths, cancellationToken);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(projectPath));
        if (!_projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            return null;

        var preferredBuildHostKind = BuildHostProcessManager.GetKindForProject(projectPath);
        var (buildHost, actualBuildHostKind) = await buildHostProcessManager.GetBuildHostWithFallbackAsync(preferredBuildHostKind, projectPath, cancellationToken);

        var loadedFile = await buildHost.LoadProjectFileAsync(projectPath, languageName, cancellationToken);
        return new RemoteProjectLoadResult
        {
            ProjectFileInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken),
            DiagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken),
            ProjectRestorePath = projectPath,
            ProjectFactory = _hostProjectFactory,
            IsFileBasedProgram = false,
            IsMiscellaneousFile = false,
            HasAllInformation = true,
            PreferredBuildHostKind = preferredBuildHostKind,
            ActualBuildHostKind = actualBuildHostKind
        };
    }
}
