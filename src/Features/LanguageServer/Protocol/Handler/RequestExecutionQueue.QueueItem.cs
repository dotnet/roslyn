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
            /// Processes the queued request. Exceptions that occur will be sent back to the requesting client, then re-thrown
            /// </summary>
            public readonly Func<RequestContext, CancellationToken, Task> CallbackAsync;

            /// <inheritdoc cref="ExportLspMethodAttribute.MutatesSolutionState" />
            public readonly bool MutatesSolutionState;

            /// <inheritdoc cref="RequestContext.ClientName" />
            public readonly string? ClientName;

            /// <inheritdoc cref="RequestContext.ClientCapabilities" />
            public readonly ClientCapabilities ClientCapabilities;

            /// <summary>
            /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
            /// </summary>
            public readonly TextDocumentIdentifier? TextDocument;

            /// <summary>
            /// A cancellation token that will cancel the handing of this request. The request could also be cancelled by the queue shutting down.
            /// </summary>
            public readonly CancellationToken CancellationToken;

            public QueueItem(bool mutatesSolutionState, ClientCapabilities clientCapabilities, string? clientName, TextDocumentIdentifier? textDocument, Func<RequestContext, CancellationToken, Task> callbackAsync, CancellationToken cancellationToken)
            {
                MutatesSolutionState = mutatesSolutionState;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                TextDocument = textDocument;
                CallbackAsync = callbackAsync;
                CancellationToken = cancellationToken;
            }
        }
    }
}
