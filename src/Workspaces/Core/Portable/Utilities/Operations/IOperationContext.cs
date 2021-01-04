// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Represents a context of executing potentially long running operation with possible wait indication providing
    /// cancellability, progress and information to the host.
    /// </summary>
    public interface IOperationContext : IDisposable
    {
        /// <summary>
        /// Cancellation token that allows user to cancel the operation unless the operation is not cancellable.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets user readable operation description, composed of initial context description and descriptions of all
        /// currently added scopes.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets current list of <see cref="IOperationScope"/>s in this context.
        /// </summary>
        IEnumerable<IOperationScope> Scopes { get; }

        /// <summary>
        /// Adds an operation scope with its own description and progress tracker. The scope is removed from the context
        /// on dispose.
        /// </summary>
        IOperationScope AddScope(string description);
    }
}
