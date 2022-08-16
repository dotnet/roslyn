// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// A placeholder type to help handle Notification messages.
/// </summary>
internal record VoidReturn
{
    public static VoidReturn Instance = new();
}

internal class QueueItem<TRequestType, TResponseType, RequestContextType> : IQueueItem<RequestContextType>
{
    public bool MutatesDocumentState { get; }

    public string MethodName { get; }

    public object? TextDocument { get; }

    public QueueItem(
        bool mutatesSolutionState,
        string methodName,
        object? textDocument,
        TRequestType request,
        IMethodHandler handler,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
    }

    public static (IQueueItem<RequestContextType>, Task<TResponseType>) Create(
        bool mutatesSolutionState,
        string methodName,
        object? textDocument,
        TRequestType request,
        IMethodHandler handler,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
    }

    /// <summary>
    /// Processes the queued request. Exceptions will be sent to the task completion source
    /// representing the task that the client is waiting for, then re-thrown so that
    /// the queue can correctly handle them depending on the type of request.
    /// </summary>
    /// <param name="context">The context for the request. If null the request will return emediatly. The context may be null when for example document context could not be resolved.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the request.</returns>
    public async Task StartRequestAsync(RequestContextType? context, CancellationToken cancellationToken)
    {
    }
}
