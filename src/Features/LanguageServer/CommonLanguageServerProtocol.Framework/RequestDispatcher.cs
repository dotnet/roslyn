// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// Aggregates handlers for the specified languages and dispatches LSP requests
/// to the appropriate handler for the request.
/// </summary>
public class RequestDispatcher<RequestContextType> : IRequestDispatcher<RequestContextType>
{
    protected ILspServices _lspServices;

    public RequestDispatcher(ILspServices lspServices)
    {
        throw new NotImplementedException();
    }

    protected virtual ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> GetRequestHandlers()
    {
        throw new NotImplementedException();
    }

    public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
        string methodName,
        TRequestType request,
        IRequestExecutionQueue<RequestContextType> queue,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteNotificationAsync<TRequestType>(string methodName, TRequestType request, IRequestExecutionQueue<RequestContextType> queue, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected virtual Task ExecuteNotificationAsync<TRequestType>(
        IRequestExecutionQueue<RequestContextType> queue,
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<TRequestType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    // TODO: Combine with the other ExecuteNotificationAsync methods probably
    public async Task ExecuteNotificationAsync(string methodName, IRequestExecutionQueue<RequestContextType> queue, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected virtual Task ExecuteNotificationAsync(IRequestExecutionQueue<RequestContextType> queue, bool mutatesSolutionState, bool requiresLSPSolution, INotificationHandler<RequestContextType> handler, string methodName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected virtual Task<TResponseType> ExecuteRequestAsync<TRequestType, TResponseType>(
        IRequestExecutionQueue<RequestContextType> queue,
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        throw new NotImplementedException();
    }
}
