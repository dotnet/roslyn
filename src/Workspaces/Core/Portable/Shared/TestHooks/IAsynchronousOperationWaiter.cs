// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

internal interface IAsynchronousOperationWaiter
{
    bool TrackActiveTokens { get; set; }
    ImmutableArray<AsynchronousOperationListener.DiagnosticAsyncToken> ActiveDiagnosticTokens { get; }

    /// <summary>
    /// Returns a task which completes when all asynchronous operations currently tracked by this waiter are
    /// completed. Asynchronous operations are expedited when possible, meaning artificial delays placed before
    /// asynchronous operations are shortened.
    /// </summary>
    Task ExpeditedWaitAsync();
    bool HasPendingWork { get; }
}
