// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Contracts;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Internal;

/// <summary>
/// LSP service factory that constructs the per-LSP-server <see cref="TestDiscoveryServiceContributor"/>,
/// which registers and proffers the source-based test discovery brokered service into the
/// <see cref="GlobalBrokeredServiceContainer"/> when the service broker is initialized.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(TestDiscoveryServiceContributor)), Shared]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[method: ImportingConstructor]
internal sealed class TestDiscoveryServiceContributorFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var workspaceRegistrationService = lspServices.GetRequiredService<LspWorkspaceRegistrationService>();
        return new TestDiscoveryServiceContributor(workspaceRegistrationService);
    }

    /// <summary>
    /// Registers and proffers the source-based test discovery brokered service. The proffered service
    /// object is the <see cref="ITestDiscoveryLanguageService"/> workspace service which also implements the
    /// rich discovery RPC interface defined in the C# Dev Kit contracts assembly. The wire descriptor is
    /// supplied by the implementation (<see cref="TestDiscoveryServiceDescriptor.Descriptor"/>), so Roslyn
    /// never needs to know the concrete RPC interface or its serialization details. When the implementation
    /// is not present (for example, a C# extension build that does not ship the Dev Kit discovery component)
    /// nothing is registered or proffered.
    /// </summary>
    private sealed class TestDiscoveryServiceContributor(LspWorkspaceRegistrationService workspaceRegistrationService)
        : ILspService
    {
        private readonly LspWorkspaceRegistrationService _workspaceRegistrationService = workspaceRegistrationService;

        public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister
        {
            get
            {
                var service = GetTestDiscoveryLanguageService();
                if (service is null)
                {
                    return ImmutableDictionary<ServiceMoniker, ServiceRegistration>.Empty;
                }

                return new Dictionary<ServiceMoniker, ServiceRegistration>
                {
                    { service.Descriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
                }.ToImmutableDictionary();
            }
        }

        public void Proffer(GlobalBrokeredServiceContainer container)
        {
            var service = GetTestDiscoveryLanguageService();
            if (service is null)
            {
                return;
            }

            // Proffer the workspace-service implementation directly as the brokered service object, using
            // the descriptor it supplies. It implements the rich discovery RPC interface (defined in C# Dev
            // Kit); Roslyn does not need to reference that interface or its serialization plumbing.
            container.Proffer(
                service.Descriptor,
                async (moniker, options, serviceBroker, cancellationToken) =>
                {
                    var testDiscoveryLanguageService = GetTestDiscoveryLanguageService();
                    if (testDiscoveryLanguageService is not null)
                    {
                        await testDiscoveryLanguageService.InitializeAsync(serviceBroker, cancellationToken).ConfigureAwait(false);
                    }
                    return testDiscoveryLanguageService;
                });
        }

        private ITestDiscoveryLanguageService? GetTestDiscoveryLanguageService()
        {
            Workspace? workspace = _workspaceRegistrationService.GetAllRegistrations().FirstOrDefault(w => w.Kind == WorkspaceKind.Host);
            Contract.ThrowIfNull(workspace, "We should always have a host workspace.");

            return workspace.Services.GetService<ITestDiscoveryLanguageService>();
        }

    }
}
