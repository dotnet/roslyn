// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[Export(typeof(IManagedHotReloadService)), PartNotDiscoverable, Shared]
internal class MockManagedHotReloadService : IManagedHotReloadService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MockManagedHotReloadService()
    {
    }

    public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation)
        => throw new NotImplementedException();

    public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation)
        => throw new NotImplementedException();

    public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation)
        => throw new NotImplementedException();

    public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation)
        => throw new NotImplementedException();
}
