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
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.HotReload;
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

internal sealed class DevKitHotReloadServiceContributor : IServiceBrokerInitializer, ILspService, IDisposable
{
    /// <summary>
    /// Per-server source text provider, observing this server's host workspace. Owned (and disposed) here so that each
    /// in-process LSP server gets its own provider bound to its own host workspace.
    /// </summary>
    private readonly PdbMatchingSourceTextProvider _sourceTextProvider;

    private readonly ManagedHotReloadLanguageService _service;
    private readonly SolutionSnapshotRegistry _solutionSnapshotRegistry;

    public DevKitHotReloadServiceContributor(
        ManagedHotReloadLanguageServiceFactory factory,
        IHostWorkspaceProvider workspaceProvider,
        SolutionSnapshotRegistry solutionSnapshotRegistry)
    {
        _solutionSnapshotRegistry = solutionSnapshotRegistry;
        _sourceTextProvider = new(workspaceProvider.Workspace);

        _service = new ManagedHotReloadLanguageService(serviceBroker =>
        {
            var solutionSnapshotProvider = new LspSolutionSnapshotProvider(serviceBroker, _solutionSnapshotRegistry);
            return factory.CreateImplementation(serviceBroker, solutionSnapshotProvider, workspaceProvider, _sourceTextProvider);
        });
    }

    public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { ManagedHotReloadUpdatesProviderDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
    }.ToImmutableDictionary();

    public void Proffer(GlobalBrokeredServiceContainer container)
        => container.Proffer(ManagedHotReloadLanguageServiceFactory.ServiceDescriptor, async (_, _, _, _) => _service);

    public ValueTask OnServiceBrokerInitializedAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken)
        => _service.InitializeAsync(serviceBroker, cancellationToken);

    public void Dispose()
        => _sourceTextProvider.Dispose();
}
