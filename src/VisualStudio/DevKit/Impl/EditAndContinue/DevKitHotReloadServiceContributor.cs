// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.DevKit.EditAndContinue;

/// <summary>
/// LSP service factory that constructs the per-LSP-server <see cref="DevKitHotReloadServiceContributor"/>,
/// which proffers the <see cref="ManagedHotReloadLanguageService"/> brokered service into the Dev Kit
/// <see cref="GlobalBrokeredServiceContainer"/> when the service broker is initialized.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(DevKitHotReloadServiceContributor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DevKitHotReloadServiceContributorFactory(
    ManagedHotReloadLanguageServiceFactory factory,
    SolutionSnapshotRegistry solutionSnapshotRegistry) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var workspaceProvider = lspServices.GetRequiredService<IHostWorkspaceProvider>();
        return new DevKitHotReloadServiceContributor(factory, workspaceProvider, solutionSnapshotRegistry);
    }
}

internal sealed class DevKitHotReloadServiceContributor(
    ManagedHotReloadLanguageServiceFactory factory,
    IHostWorkspaceProvider workspaceProvider,
    SolutionSnapshotRegistry solutionSnapshotRegistry) : IServiceBrokerInitializer, ILspService
{
    public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { ManagedHotReloadLanguageServiceDescriptor.Descriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
    }.ToImmutableDictionary();

    public void Proffer(GlobalBrokeredServiceContainer container)
    {
        var serviceBroker = container.GetFullAccessServiceBroker();
        var solutionSnapshotProvider = new LspSolutionSnapshotProvider(serviceBroker, solutionSnapshotRegistry);

        container.Proffer(
            ManagedHotReloadLanguageServiceDescriptor.Descriptor,
            (moniker, options, innerServiceBroker, cancellationToken) =>
            {
                var service = factory.Create(serviceBroker, solutionSnapshotProvider, workspaceProvider);
                return new ValueTask<object?>(service);
            });
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
    }
}
