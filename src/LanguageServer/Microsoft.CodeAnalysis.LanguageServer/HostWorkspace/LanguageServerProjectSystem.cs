// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem : LanguageServerProjectLoader
{
    private readonly ILogger _logger;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;

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
        IBinLogPathProvider binLogPathProvider)
            : base(
                workspaceFactory.HostProjectFactory,
                workspaceFactory.TargetFrameworkManager,
                workspaceFactory.ProjectSystemHostInfo,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));
        var workspace = ProjectFactory.Workspace;
        _projectFileExtensionRegistry = new ProjectFileExtensionRegistry(workspace.CurrentSolution.Services, new DiagnosticReporter(workspace));
    }

    public async Task OpenSolutionAsync(string solutionFilePath)
    {
        _logger.LogInformation(string.Format(LanguageServerResources.Loading_0, solutionFilePath));
        ProjectFactory.SolutionPath = solutionFilePath;

        // We'll load solutions out-of-proc, since it's possible we might be running on a runtime that doesn't have a matching SDK installed,
        // and we don't want any MSBuild registration to set environment variables in our process that might impact child processes.
        await using var buildHostProcessManager = new BuildHostProcessManager(globalMSBuildProperties: AdditionalProperties, loggerFactory: LoggerFactory);
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(BuildHostProcessKind.NetCore, CancellationToken.None);

        // If we don't have a .NET Core SDK on this machine at all, try .NET Framework
        if (!await buildHost.HasUsableMSBuildAsync(solutionFilePath, CancellationToken.None))
        {
            var kind = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? BuildHostProcessKind.NetFramework : BuildHostProcessKind.Mono;
            buildHost = await buildHostProcessManager.GetBuildHostAsync(kind, CancellationToken.None);
        }

        var projects = await buildHost.GetProjectsInSolutionAsync(solutionFilePath, CancellationToken.None);
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

    protected override async Task<(RemoteProjectFile projectFile, bool hasAllInformation, BuildHostProcessKind preferred, BuildHostProcessKind actual)?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        if (!_projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            return null;

        var preferredBuildHostKind = GetKindForProject(projectPath);
        var (buildHost, actualBuildHostKind) = await buildHostProcessManager.GetBuildHostWithFallbackAsync(preferredBuildHostKind, projectPath, cancellationToken);

        var loadedFile = await buildHost.LoadProjectFileAsync(projectPath, languageName, cancellationToken);
        return (loadedFile, hasAllInformation: true, preferredBuildHostKind, actualBuildHostKind);
    }
}
