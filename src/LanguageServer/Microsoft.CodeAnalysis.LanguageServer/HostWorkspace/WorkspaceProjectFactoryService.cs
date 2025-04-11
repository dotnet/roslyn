// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
/// <summary>
/// An implementation of the brokered service <see cref="IWorkspaceProjectFactoryService"/> that just maps calls to the underlying project system.
/// </summary>
[ExportBrokeredService("Microsoft.VisualStudio.LanguageServices.WorkspaceProjectFactoryService", null, Audience = ServiceAudience.Local)]
internal class WorkspaceProjectFactoryService : IWorkspaceProjectFactoryService, IExportedBrokeredService
{
    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ProjectInitializationHandler _projectInitializationHandler;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceProjectFactoryService(LanguageServerWorkspaceFactory workspaceFactory, ProjectInitializationHandler projectInitializationHandler, ILoggerFactory loggerFactory)
    {
        _workspaceFactory = workspaceFactory;
        _projectInitializationHandler = projectInitializationHandler;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(nameof(WorkspaceProjectFactoryService));
    }

    ServiceRpcDescriptor IExportedBrokeredService.Descriptor => WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor;

    async Task IExportedBrokeredService.InitializeAsync(CancellationToken cancellationToken)
    {
        await _projectInitializationHandler.SubscribeToInitializationCompleteAsync(cancellationToken);
    }

    public async Task<IWorkspaceProject> CreateAndAddProjectAsync(WorkspaceProjectCreationInfo creationInfo, CancellationToken _)
    {
        _logger.LogInformation(string.Format(LanguageServerResources.Project_0_loaded_by_CSharp_Dev_Kit, creationInfo.FilePath));
        VSCodeRequestTelemetryLogger.ReportProjectLoadStarted();
        try
        {
            if (creationInfo.BuildSystemProperties.TryGetValue("SolutionPath", out var solutionPath))
            {
                _workspaceFactory.ProjectSystemProjectFactory.SolutionPath = solutionPath;
            }

            var project = await _workspaceFactory.ProjectSystemProjectFactory.CreateAndAddToWorkspaceAsync(
                creationInfo.DisplayName,
                creationInfo.Language,
                new Workspaces.ProjectSystem.ProjectSystemProjectCreationInfo { FilePath = creationInfo.FilePath },
                _workspaceFactory.ProjectSystemHostInfo);

            var workspaceProject = new WorkspaceProject(project, _workspaceFactory.Workspace.Services.SolutionServices, _workspaceFactory.TargetFrameworkManager, _loggerFactory);

            // We've created a new project, so initialize properties we have
            await workspaceProject.SetBuildSystemPropertiesAsync(creationInfo.BuildSystemProperties, CancellationToken.None);

            return workspaceProject;
        }
        catch (Exception e) when (LanguageServerFatalError.ReportAndLogAndPropagate(e, _logger, $"Failed to create project {creationInfo.DisplayName}"))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public Task<IReadOnlyCollection<string>> GetSupportedBuildSystemPropertiesAsync(CancellationToken _)
    {
        // TODO: implement
        return Task.FromResult((IReadOnlyCollection<string>)[]);
    }
}
#pragma warning restore RS0030 // Do not used banned APIs
