// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework;

#nullable enable

public interface IQueueItem<RequestContextType> where RequestContextType : struct
{
    /// <summary>
    /// Begins executing the work specified by this queue item.
    /// </summary>
    Task CallbackAsync(RequestContextType? context, CancellationToken cancellationToken);

    /// <inheritdoc cref="IRequestHandler{RequestContextType}.RequiresLSPSolution" />
    bool RequiresLSPSolution { get; }

    /// <inheritdoc cref="IRequestHandler{RequestContextType}.MutatesSolutionState" />
    bool MutatesSolutionState { get; }

    string MethodName { get; }

    /// <summary>
    /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType, TResponseContextType}.GetTextDocumentIdentifier(RequestType)"/>.
    /// </summary>
    TextDocumentIdentifier? TextDocument { get; }

    ClientCapabilities ClientCapabilities { get; }

    /// <summary>
    /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
    /// tracing/logging subsystem.
    /// </summary>
    Guid ActivityId { get; }

    IRequestMetrics Metrics { get; }
}
