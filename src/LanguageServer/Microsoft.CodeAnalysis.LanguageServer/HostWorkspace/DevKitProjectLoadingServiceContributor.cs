// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
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
[Export(typeof(IServiceBrokerInitializer)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DevKitProjectLoadingServiceContributor(
    LanguageServerWorkspaceFactory workspaceFactory,
    ILoggerFactory loggerFactory) : IServiceBrokerInitializer
{
    private ProjectInitializationHandler? _projectInitializationHandler;

    public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
    }.ToImmutableDictionary();

    public void Proffer(GlobalBrokeredServiceContainer container)
    {
        var serviceBroker = container.GetFullAccessServiceBroker();
        _projectInitializationHandler = new ProjectInitializationHandler(serviceBroker, loggerFactory);

        container.Proffer(
            WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor,
            async (moniker, options, innerServiceBroker, cancellationToken) =>
            {
                var service = new WorkspaceProjectFactoryService(workspaceFactory, _projectInitializationHandler, loggerFactory);
                await service.InitializeAsync(cancellationToken);
                return service;
            });
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
    }
}
