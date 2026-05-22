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
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem : LanguageServerProjectLoader
{
    private readonly ILogger _logger;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;
    private readonly ProjectSystemProjectFactory _hostProjectFactory;

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

    public async Task OpenSolutionAsync(string solutionFilePath, IProgress<LSP.WorkDoneProgress>? progressReporter = null)
    {
        _logger.LogInformation(string.Format(LanguageServerResources.Loading_0, solutionFilePath));
        _hostProjectFactory.SolutionPath = solutionFilePath;

        var (_, projects) = await SolutionFileReader.ReadSolutionFileAsync(solutionFilePath, DiagnosticReportingMode.Throw, CancellationToken.None);

        await using var progressTracker = progressReporter != null && projects.Length > 0
            ? new ProjectLoadProgressTracker(progressReporter, projects.Length)
            : null;

        foreach (var (path, guid) in projects)
        {
            await BeginLoadingProjectAsync(path, guid, progressTracker);
        }

        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
    }

    public async Task OpenProjectsAsync(ImmutableArray<string> projectFilePaths, IProgress<LSP.WorkDoneProgress>? progressReporter = null)
    {
        if (!projectFilePaths.Any())
            return;

        await using var progressTracker = progressReporter != null && projectFilePaths.Length > 0
            ? new ProjectLoadProgressTracker(progressReporter, projectFilePaths.Length)
            : null;

        foreach (var path in projectFilePaths)
        {
            await BeginLoadingProjectAsync(path, projectGuid: null, progressTracker);
        }

        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
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
