// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class ManagedHotReloadServiceBridge(IManagedHotReloadService service) : InternalContracts.IManagedHotReloadService
    {
        public async ValueTask<ImmutableArray<InternalContracts.ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation)
            => (await service.GetActiveStatementsAsync(cancellation).ConfigureAwait(false)).SelectAsArray(a => a.ToContract());

        public async ValueTask<InternalContracts.ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation)
            => (await service.GetAvailabilityAsync(module, cancellation).ConfigureAwait(false)).ToContract();

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation)
            => service.GetCapabilitiesAsync(cancellation);

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation)
            => service.PrepareModuleForUpdateAsync(module, cancellation);
    }
}
