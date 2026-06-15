// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Registers and proffers the <see cref="WorkspaceProjectFactoryService"/> brokered service
/// into the Dev Kit <see cref="GlobalBrokeredServiceContainer"/> when the service broker is initialized.
/// Also creates the <see cref="ProjectInitializationHandler"/> with the actual service broker instance
/// so it can subscribe to the remote project initialization status service.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(DevKitProjectLoadingServiceContributor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DevKitProjectLoadingServiceContributorFactory(
    ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new DevKitProjectLoadingServiceContributor(lspServices, loggerFactory);
}

internal sealed class DevKitProjectLoadingServiceContributor(
    LspServices lspServices,
    ILoggerFactory loggerFactory) : IServiceBrokerInitializer, ILspService
{
    public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
    }.ToImmutableDictionary();

    public void Proffer(GlobalBrokeredServiceContainer container)
    {
        container.Proffer(
            WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor,
            async (moniker, options, innerServiceBroker, cancellationToken) =>
            {
                var workspaceFactory = lspServices.GetRequiredService<LanguageServerWorkspaceFactory>();
                var targetFrameworkManager = lspServices.GetRequiredService<ProjectTargetFrameworkManager>();
                var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
                var projectInitializationHandler = new ProjectInitializationHandler(clientLanguageServerManager, innerServiceBroker, loggerFactory);
                var service = new WorkspaceProjectFactoryService(workspaceFactory, targetFrameworkManager, projectInitializationHandler, loggerFactory);
                await service.InitializeAsync(cancellationToken);
                return service;
            });
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
    }
}
