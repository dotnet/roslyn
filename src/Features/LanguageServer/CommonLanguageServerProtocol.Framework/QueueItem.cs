// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;

namespace CommonLanguageServerProtocol.Framework;

public class QueueItem<TRequestType, TResponseType, RequestContextType> : IQueueItem<RequestContextType>
{
    public bool RequiresLSPSolution { get; }

    public bool MutatesSolutionState { get; }

    public string MethodName { get; }

    public object? TextDocument { get; }

    public QueueItem(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        IRequestHandler handler,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static (IQueueItem<RequestContextType>, Task<TResponseType>) Create(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        IRequestHandler handler,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Processes the queued request. Exceptions will be sent to the task completion source
    /// representing the task that the client is waiting for, then re-thrown so that
    /// the queue can correctly handle them depending on the type of request.
    /// </summary>
    public async Task CallbackAsync(RequestContextType? context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //TODO: Should we delete this?
    public virtual void OnExecutionStart()
    {
    }
}
