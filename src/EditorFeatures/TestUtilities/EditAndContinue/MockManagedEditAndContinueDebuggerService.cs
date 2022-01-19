﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockManagedEditAndContinueDebuggerService : IManagedHotReloadService
    {
        public Func<Guid, ManagedHotReloadAvailability>? IsEditAndContinueAvailable;
        public Dictionary<Guid, ManagedHotReloadAvailability>? LoadedModules;
        public Func<ImmutableArray<ManagedActiveStatementDebugInfo>>? GetActiveStatementsImpl;
        public Func<ImmutableArray<string>>? GetCapabilitiesImpl;

        public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetActiveStatementsImpl?.Invoke() ?? ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

        public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            if (IsEditAndContinueAvailable != null)
            {
                return ValueTaskFactory.FromResult(IsEditAndContinueAvailable(mvid));
            }

            if (LoadedModules != null)
            {
                return ValueTaskFactory.FromResult(LoadedModules.TryGetValue(mvid, out var result) ? result : new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.ModuleNotLoaded));
            }

            throw new NotImplementedException();
        }

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(GetCapabilitiesImpl?.Invoke() ?? ImmutableArray.Create("Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"));

        public ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            => ValueTaskFactory.CompletedTask;
    }
}
