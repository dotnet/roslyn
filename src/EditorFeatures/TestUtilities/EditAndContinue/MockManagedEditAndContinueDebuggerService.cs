// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockManagedEditAndContinueDebuggerService : IManagedEditAndContinueDebuggerService
    {
        public Func<Guid, ManagedEditAndContinueAvailability>? IsEditAndContinueAvailable;
        public Dictionary<Guid, ManagedEditAndContinueAvailability>? LoadedModules;
        public Func<ImmutableArray<ManagedActiveStatementDebugInfo>>? GetActiveStatementsImpl;

        public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetActiveStatementsImpl?.Invoke() ?? ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

        public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            if (IsEditAndContinueAvailable != null)
            {
                return Task.FromResult(IsEditAndContinueAvailable(mvid));
            }

            if (LoadedModules != null)
            {
                return Task.FromResult(LoadedModules.TryGetValue(mvid, out var result) ? result : new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.ModuleNotLoaded));
            }

            throw new NotImplementedException();
        }

        public Task<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create("Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"));

        public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
