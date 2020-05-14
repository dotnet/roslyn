// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Provides current active statements.
    /// </summary>
    internal interface IActiveStatementProvider
    {
        /// <summary>
        /// Retrieves active statements from the debuggee process.
        /// Shall only be called while in debug mode.
        /// Can be invoked on any thread.
        /// </summary>
        Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken);
    }
}
