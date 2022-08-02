// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Collections.Immutable;

#nullable enable

namespace CommonLanguageServerProtocol.Framework;

public class RequestExecutionQueue<RequestContextType> : IRequestExecutionQueue<RequestContextType>
{
    protected readonly string _serverKind;
    protected readonly ILspLogger _logger;

    protected readonly AsyncQueue<(IQueueItem<RequestContextType> queueItem, CancellationToken cancellationToken)> _queue = new();

    protected Task? _queueProcessingTask;

    public CancellationToken CancellationToken => _cancelSource.Token;

    public event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

    public RequestExecutionQueue(
        string serverKind,
        ILspLogger logger)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }

    public void Start(ILspServices lspServices)
    {
        throw new NotImplementedException();
    }


    public Task ExecuteAsync(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<RequestContextType> handler,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync<TRequestType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<TRequestType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        throw new NotImplementedException();
    }
    
    public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual (IQueueItem<RequestContextType>, Task<TResponseType>) CreateQueueItem<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual (IQueueItem<RequestContextType>, Task) CreateQueueItem<TRequestType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        INotificationHandler<TRequestType, RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual (IQueueItem<RequestContextType>, Task) CreateQueueItem(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        INotificationHandler<RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected virtual IRequestContextFactory<RequestContextType> GetRequestContextFactory(ILspServices lspServices)
    {
        throw new NotImplementedException();
    }
}
