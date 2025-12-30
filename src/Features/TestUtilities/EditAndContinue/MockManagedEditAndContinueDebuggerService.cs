// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class MockManagedEditAndContinueDebuggerService : IManagedHotReloadService
{
    public Func<Guid, ManagedHotReloadAvailability>? IsEditAndContinueAvailable;
    public Dictionary<Guid, ManagedHotReloadAvailability>? LoadedModules;
    public Func<ImmutableArray<ManagedActiveStatementDebugInfo>>? GetActiveStatementsImpl;
    public Func<ImmutableArray<string>>? GetCapabilitiesImpl;

    public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        => GetActiveStatementsImpl?.Invoke() ?? [];

    public async ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
    {
        if (IsEditAndContinueAvailable != null)
        {
            return IsEditAndContinueAvailable(mvid);
        }

        if (LoadedModules != null)
        {
            return LoadedModules.TryGetValue(mvid, out var result) ? result : new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.ModuleNotLoaded);
        }

        throw new NotImplementedException();
    }

    public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => GetCapabilitiesImpl?.Invoke() ?? ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"];

    public ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
