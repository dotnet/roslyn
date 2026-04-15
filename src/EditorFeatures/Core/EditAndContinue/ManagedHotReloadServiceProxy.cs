// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(InternalContracts.IManagedHotReloadService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ManagedHotReloadServiceProxy(IServiceBrokerProvider serviceBrokerProvider) :
    BrokeredServiceProxy<IManagedHotReloadService>(
        serviceBrokerProvider.ServiceBroker,
        BrokeredServiceDescriptors.DebuggerManagedHotReloadService,
        BrokeredServiceDescriptors.DebuggerManagedHotReloadServiceLegacy),
    InternalContracts.IManagedHotReloadService
{
    public async ValueTask<ImmutableArray<InternalContracts.ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
    {
        var result = await InvokeAsync((service, cancellationToken) => service.GetActiveStatementsAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
        return result.SelectAsArray(info => info.ToContract());
    }

    public async ValueTask<InternalContracts.ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
    {
        var result = await InvokeAsync((service, module, cancellationToken) => service.GetAvailabilityAsync(module, cancellationToken), module, cancellationToken).ConfigureAwait(false);
        return result.ToContract();
    }

    public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => InvokeAsync((service, cancellationToken) => service.GetCapabilitiesAsync(cancellationToken), cancellationToken);

    public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
        => InvokeAsync((service, module, cancellationToken) => service.PrepareModuleForUpdateAsync(module, cancellationToken), module, cancellationToken);
}
