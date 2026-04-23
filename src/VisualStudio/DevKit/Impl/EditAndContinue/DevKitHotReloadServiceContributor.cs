// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.DevKit.EditAndContinue;

/// <summary>
/// Registers and proffers the <see cref="ManagedHotReloadLanguageService"/> brokered service
/// into the Dev Kit <see cref="GlobalBrokeredServiceContainer"/> when the service broker is initialized.
/// </summary>
[Export(typeof(IServiceBrokerInitializer))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DevKitHotReloadServiceContributor(
    ManagedHotReloadLanguageServiceFactory factory,
    SolutionSnapshotRegistry solutionSnapshotRegistry) : IServiceBrokerInitializer
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
                var service = factory.Create(serviceBroker, solutionSnapshotProvider);
                return new ValueTask<object?>(service);
            });
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
    }
}
