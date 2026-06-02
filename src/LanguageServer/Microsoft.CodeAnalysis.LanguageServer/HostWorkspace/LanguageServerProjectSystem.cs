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
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider listenerProvider,
    ServerConfigurationFactory serverConfigurationFactory,
    IBinLogPathProvider binLogPathProvider,
    DotnetCliHelper dotnetCliHelper) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new LanguageServerProjectSystem(
            lspServices,
            globalOptionService,
            loggerFactory,
            listenerProvider,
            serverConfigurationFactory,
            binLogPathProvider,
            dotnetCliHelper);
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
