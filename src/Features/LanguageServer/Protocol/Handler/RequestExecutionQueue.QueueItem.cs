// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly struct QueueItem
        {
            public readonly Func<RequestContext, CancellationToken, Task<bool>> Callback;
            public readonly bool MutatesSolutionState;
            public readonly string? ClientName;
            public readonly ClientCapabilities ClientCapabilities;

            public QueueItem(bool mutatesSolutionState, ClientCapabilities clientCapabilities, string? clientName, Func<RequestContext, Task<bool>> callback)
            {
                MutatesSolutionState = mutatesSolutionState;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                Callback = callback;
            }
        }
    }
}
