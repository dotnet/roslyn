// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class ManagedHotReloadServiceProxy(IServiceBroker serviceBroker) :
    BrokeredServiceProxy<IManagedHotReloadService>(serviceBroker, BrokeredServiceDescriptors.DebuggerManagedHotReloadService),
    IManagedHotReloadService
{
    public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        => InvokeAsync((service, cancellationToken) => service.GetActiveStatementsAsync(cancellationToken), cancellationToken);

    public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
        => InvokeAsync((service, module, cancellationToken) => service.GetAvailabilityAsync(module, cancellationToken), module, cancellationToken);

    public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => InvokeAsync((service, cancellationToken) => service.GetCapabilitiesAsync(cancellationToken), cancellationToken);

    public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
        => InvokeAsync((service, module, cancellationToken) => service.PrepareModuleForUpdateAsync(module, cancellationToken), module, cancellationToken);
}
