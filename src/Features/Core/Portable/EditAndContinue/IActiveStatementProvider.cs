// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
