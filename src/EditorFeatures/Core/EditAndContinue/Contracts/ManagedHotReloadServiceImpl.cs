﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

using Contracts = Microsoft.CodeAnalysis.EditAndContinue.Contracts;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class ManagedHotReloadServiceImpl : Contracts.IManagedHotReloadService
    {
        private readonly IManagedHotReloadService _service;

        public ManagedHotReloadServiceImpl(IManagedHotReloadService service)
            => _service = service;

        public async ValueTask<ImmutableArray<Contracts.ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation)
            => (await _service.GetActiveStatementsAsync(cancellation).ConfigureAwait(false)).SelectAsArray(a => a.ToContract());

        public async ValueTask<Contracts.ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation)
            => (await _service.GetAvailabilityAsync(module, cancellation).ConfigureAwait(false)).ToContract();

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation)
            => _service.GetCapabilitiesAsync(cancellation);

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation)
            => _service.PrepareModuleForUpdateAsync(module, cancellation);
    }
}
