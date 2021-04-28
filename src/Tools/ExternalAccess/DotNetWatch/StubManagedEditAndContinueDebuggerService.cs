// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal class StubManagedEditAndContinueDebuggerService : IManagedEditAndContinueDebuggerService
    {
        public static readonly StubManagedEditAndContinueDebuggerService Instance = new();

        private StubManagedEditAndContinueDebuggerService() { }

        public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

        public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid moduleVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available));
        }

        public Task<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create("Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"));
        }

        public Task PrepareModuleForUpdateAsync(Guid moduleVersionId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
