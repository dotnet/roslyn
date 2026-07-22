// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// LSP service factory that constructs the per-LSP-server <see cref="LanguageServerProjectSystem"/>.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(LanguageServerProjectSystem)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerProjectSystemServiceFactory(
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListenerProvider listenerProvider,
    ServerConfigurationFactory serverConfigurationFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new LanguageServerProjectSystem(
            lspServices,
            globalOptionService,
            lspServices.GetRequiredService<ILoggerFactory>(),
            listenerProvider,
            serverConfigurationFactory,
            lspServices.GetRequiredService<IBinLogPathProvider>(),
            lspServices.GetRequiredService<DotnetCliHelper>());
}

internal sealed class LanguageServerProjectSystem : LanguageServerProjectLoader, ILspService
{
    private readonly ILogger _logger;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;
    private readonly ProjectSystemProjectFactory _hostProjectFactory;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;

    public LanguageServerProjectSystem(
        ILspServices lspServices,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
            : base(
                lspServices,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));
        _hostProjectFactory = lspServices.GetRequiredService<LanguageServerWorkspaceFactory>().HostProjectFactory;
        _clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        var workspace = _hostProjectFactory.Workspace;
        _projectFileExtensionRegistry = new ProjectFileExtensionRegistry(new DiagnosticReporter(workspace));
    }

    /// <summary>
    /// When a solution has been opened (either explicitly or via auto-load), restore the solution as a whole rather
    /// than restoring each contained project individually. A single solution-level restore is significantly faster than
    /// running <c>dotnet restore</c> once per project, which matters most for large, completely unrestored solutions.
    /// </summary>
    /// <remarks>
    /// A solution-level restore only covers the projects contained in the solution. It is possible for a project to be
    /// loaded into this project system without being part of the open solution (for example, a project opened on its own
    /// via <see cref="OpenProjectsAsync"/>). Such projects are not covered by the solution restore, so they are still
    /// restored individually alongside the solution. The solution's project set is re-read from disk here (rather than
    /// cached) so that edits to the solution file are always reflected in the restore scope.
    /// </remarks>
    protected override async ValueTask<ImmutableArray<string>> GetPathsToRestoreAsync(ImmutableArray<string> projectsThatNeedRestore, CancellationToken cancellationToken)
    {
        var solutionPath = _hostProjectFactory.SolutionPath;

        // If no solution is open, restore each project individually.
        if (solutionPath is null)
            return projectsThatNeedRestore;

        // Re-read the solution's current project set so a solution-level restore only collapses projects that are
        // actually part of the solution as it exists on disk right now (the set can change if the solution file is
        // edited between restores).
        ImmutableHashSet<string> solutionProjectPaths;
        try
        {
            var (_, projects) = await SolutionFileReader.ReadSolutionFileAsync(solutionPath, DiagnosticReportingMode.Throw, cancellationToken);
            solutionProjectPaths = projects.Select(static p => p.ProjectPath).ToImmutableHashSet(PathUtilities.Comparer);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // If the solution can't be read (for example it was edited into an invalid state), fall back to restoring
            // each project individually rather than failing the restore entirely.
            _logger.LogWarning(e, "Unable to read solution '{SolutionPath}' to determine restore scope; restoring {ProjectCount} project(s) individually.", solutionPath, projectsThatNeedRestore.Length);
            return projectsThatNeedRestore;
        }

        // Separate out any projects that need a restore but are not part of the open solution. Those are not covered by
        // a solution-level restore, so they must still be restored on their own.
        var projectsNotInSolution = projectsThatNeedRestore.WhereAsArray(
            static (path, solutionProjectPaths) => !solutionProjectPaths.Contains(path), solutionProjectPaths);

        // If none of the projects that need a restore are actually part of the solution, restore them individually
        // rather than kicking off a solution restore that would not cover any of them.
        if (projectsNotInSolution.Length == projectsThatNeedRestore.Length)
            return projectsThatNeedRestore;

        if (projectsNotInSolution.IsEmpty)
            _logger.LogInformation("Restoring solution '{SolutionPath}' instead of {ProjectCount} individual project(s).", solutionPath, projectsThatNeedRestore.Length);
        else
            _logger.LogInformation("Restoring solution '{SolutionPath}' for its projects, plus {ProjectCount} project(s) not contained in the solution.", solutionPath, projectsNotInSolution.Length);

        // Restore the solution as a whole (covering all of its projects), plus any projects outside the solution.
        return [solutionPath, .. projectsNotInSolution];
    }

    public async Task OpenSolutionAsync(string solutionFilePath, IProgress<LSP.WorkDoneProgress>? progressReporter = null)
    {
        _logger.LogInformation(string.Format(LanguageServerResources.Loading_0, solutionFilePath));
        _hostProjectFactory.SolutionPath = solutionFilePath;

        var (_, projects) = await SolutionFileReader.ReadSolutionFileAsync(solutionFilePath, DiagnosticReportingMode.Throw, CancellationToken.None);

        await using var progressTracker = progressReporter != null && projects.Length > 0
            ? new WorkDoneProgressTracker(progressReporter, projects.Length)
            : null;

        foreach (var (path, guid) in projects)
        {
            await BeginLoadingProjectAsync(path, guid, progressTracker);
        }

        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync(_clientLanguageServerManager);
    }

    public async Task OpenProjectsAsync(ImmutableArray<string> projectFilePaths, IProgress<LSP.WorkDoneProgress>? progressReporter = null)
    {
        if (!projectFilePaths.Any())
            return;

        await using var progressTracker = progressReporter != null && projectFilePaths.Length > 0
            ? new WorkDoneProgressTracker(progressReporter, projectFilePaths.Length)
            : null;

        foreach (var path in projectFilePaths)
        {
            await BeginLoadingProjectAsync(path, projectGuid: null, progressTracker);
        }

        await WaitForProjectsToFinishLoadingAsync();
        await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync(_clientLanguageServerManager);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(projectPath));
        if (!_projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            return null;

        var preferredBuildHostKind = BuildHostProcessManager.GetKindForProject(projectPath);
        var (buildHost, actualBuildHostKind) = await buildHostProcessManager.GetBuildHostWithFallbackAsync(preferredBuildHostKind, projectPath, cancellationToken);

        await using var loadedFile = await buildHost.LoadProjectFileAsync(projectPath, languageName, cancellationToken);
        return new RemoteProjectLoadResult
        {
            ProjectFileInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken),
            DiagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken),
            ProjectRestorePath = projectPath,
            ProjectFactory = _hostProjectFactory,
            IsFileBasedProgram = false,
            HasFileBasedAppDirectives = false,
            IsMiscellaneousFile = false,
            HasAllInformation = true,
            PreferredBuildHostKind = preferredBuildHostKind,
            ActualBuildHostKind = actualBuildHostKind
        };
    }
}
