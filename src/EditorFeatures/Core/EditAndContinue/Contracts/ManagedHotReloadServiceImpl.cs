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
    internal sealed class ManagedHotReloadServiceImpl(IManagedHotReloadService service) : InternalContracts.IManagedHotReloadService
    {
        private readonly IManagedHotReloadService _service = service;

        public async ValueTask<ImmutableArray<InternalContracts.ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation)
            => (await _service.GetActiveStatementsAsync(cancellation).ConfigureAwait(false)).SelectAsArray(a => a.ToContract());

        public async ValueTask<InternalContracts.ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation)
            => (await _service.GetAvailabilityAsync(module, cancellation).ConfigureAwait(false)).ToContract();

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation)
            => _service.GetCapabilitiesAsync(cancellation);

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation)
            => _service.PrepareModuleForUpdateAsync(module, cancellation);
    }
}
