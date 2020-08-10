// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly struct QueueItem
        {
            /// <summary>
            /// Processes the queued request, and signals back to the called whether the handler ran to completion.
            /// </summary>
            /// <remarks>A return value of true does not imply that the request was handled successfully, only that no exception was thrown and the task wasn't cancelled.</remarks>
            public readonly Func<RequestContext, CancellationToken, Task<bool>> CallbackAsync;
            public readonly bool MutatesSolutionState;
            public readonly string? ClientName;
            public readonly ClientCapabilities ClientCapabilities;
            public readonly TextDocumentIdentifier? TextDocument;
            public readonly CancellationToken CancellationToken;

            public QueueItem(bool mutatesSolutionState, ClientCapabilities clientCapabilities, string? clientName, TextDocumentIdentifier? textDocument, Func<RequestContext, CancellationToken, Task<bool>> callback, CancellationToken cancellationToken)
            {
                MutatesSolutionState = mutatesSolutionState;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                TextDocument = textDocument;
                CallbackAsync = callback;
                CancellationToken = cancellationToken;
            }
        }
    }
}
