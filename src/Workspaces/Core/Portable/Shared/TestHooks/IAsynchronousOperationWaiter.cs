// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IAsynchronousOperationWaiter
    {
        bool TrackActiveTokens { get; set; }
        ImmutableArray<AsynchronousOperationListener.DiagnosticAsyncToken> ActiveDiagnosticTokens { get; }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        Task CreateWaitTask();
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        bool HasPendingWork { get; }
    }
}
