// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
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
    private readonly LanguageServerProjectSystem _projectSystem;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceProjectFactoryService(LanguageServerProjectSystem projectSystem)
    {
        _projectSystem = projectSystem;
    }

    ServiceRpcDescriptor IExportedBrokeredService.Descriptor => WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor;

    Task IExportedBrokeredService.InitializeAsync(CancellationToken cancellationToken)
    {
        // There's nothing to initialize
        return Task.CompletedTask;
    }

    public async Task<IWorkspaceProject> CreateAndAddProjectAsync(WorkspaceProjectCreationInfo creationInfo, CancellationToken _)
    {
        var project = await _projectSystem.ProjectSystemProjectFactory.CreateAndAddToWorkspaceAsync(
            creationInfo.DisplayName,
            creationInfo.Language,
            new Workspaces.ProjectSystem.ProjectSystemProjectCreationInfo { FilePath = creationInfo.FilePath },
            _projectSystem.ProjectSystemHostInfo);

        var workspaceProject = new WorkspaceProject(project, _projectSystem.Workspace.Services.SolutionServices);

        // We've created a new project, so initialize properties we have
        await workspaceProject.SetBuildSystemPropertiesAsync(creationInfo.BuildSystemProperties, CancellationToken.None);

        return workspaceProject;
    }

    public Task<IReadOnlyCollection<string>> GetSupportedBuildSystemPropertiesAsync(CancellationToken _)
    {
        // TODO: implement
        return Task.FromResult((IReadOnlyCollection<string>)ImmutableArray<string>.Empty);
    }
}
#pragma warning restore RS0030 // Do not used banned APIs
