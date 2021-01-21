// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    internal interface IManagedEditAndContinueDebuggerService
    {
        Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether EnC is allowed for all loaded instances of module with specified <paramref name="moduleVersionId"/>.
        /// </summary>
        Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid moduleVersionId, CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the debugger that a document changed that may affect the given module when the change is applied.
        /// </summary>
        Task PrepareModuleForUpdateAsync(Guid moduleVersionId, CancellationToken cancellationToken);
    }
}
