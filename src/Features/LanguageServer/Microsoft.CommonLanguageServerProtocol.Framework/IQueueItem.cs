// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An item to be queued for execution.
/// </summary>
/// <typeparam name="RequestContextType">The type of the request context to be passed along to the handler.</typeparam>
public interface IQueueItem<RequestContextType>
{
    /// <summary>
    /// Begins executing the work specified by this queue item.
    /// </summary>
    Task StartRequestAsync(RequestContextType context, CancellationToken cancellationToken);

    /// <summary>
    /// Indicates that this request may mutate the document state, so that the queue may handle its execution appropriatly.
    /// </summary>
    bool MutatesDocumentState { get; }

    /// <summary>
    /// The method being executed.
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// The document identifier that will be used to find the solution and document for this request.
    /// This comes from the TextDocumentIdentifier returned from the handler itself via a call to 
    /// <see cref="IRequestHandler{RequestType, ResponseType, TResponseContextType}.GetTextDocumentIdentifier(RequestType)"/>.
    /// </summary>
    object? TextDocument { get; }
}
