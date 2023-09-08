// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.BrokeredServices;

#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
/// <summary>
/// An implementation of the brokered service <see cref="ILanguageServerProjectSystemService"/> that just maps calls to the underlying project system.
/// </summary>
[ExportBrokeredService("Microsoft.VisualStudio.LanguageServices.LanguageServerProjectSystemService", null, Audience = ServiceAudience.Local)]
internal sealed class LanguageServerProjectSystemService : ILanguageServerProjectSystemService, IExportedBrokeredService
{
    private readonly LanguageServerProjectSystem _projectSystem;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerProjectSystemService(LanguageServerProjectSystem projectSystem, ProjectInitializationHandler projectInitializationHandler)
    {
        _projectSystem = projectSystem;
    }

    ServiceRpcDescriptor IExportedBrokeredService.Descriptor => LanguageServerProjectSystemServiceDescriptor.ServiceDescriptor;

    Task IExportedBrokeredService.InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    async Task<ILoadedProjectSystemProject> ILanguageServerProjectSystemService.LoadProjectAsync(string projectFilePath, ImmutableDictionary<string, string> buildProperties, CancellationToken cancellationToken)
    {
        // TODO: pass through the build properties, which we don't have support for yet
        await _projectSystem.OpenProjectsAsync(ImmutableArray.Create(projectFilePath));

        // TODO: pass something along here so we can unload
        return new LoadedProjectSystemProject();
    }
}
#pragma warning restore RS0030 // Do not used banned APIs
