// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IAsynchronousOperationWaiter
    {
        bool TrackActiveTokens { get; set; }
        ImmutableArray<AsynchronousOperationListener.DiagnosticAsyncToken> ActiveDiagnosticTokens { get; }

        /// <summary>
        /// Returns a task which completes when all asynchronous operations currently tracked by this waiter are
        /// completed. Asynchronous operations are expedited when possible, meaning artificial delays placed before
        /// asynchronous operations are shortened.
        /// </summary>
        Task CreateExpeditedWaitTask();
        bool HasPendingWork { get; }
    }
}
