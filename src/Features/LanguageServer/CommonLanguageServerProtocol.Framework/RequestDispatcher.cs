// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// Aggregates handlers for the specified languages and dispatches LSP requests
/// to the appropriate handler for the request.
/// </summary>
public abstract class RequestDispatcher<RequestContextType> : IRequestDispatcher<RequestContextType> where RequestContextType : struct
{
    protected ILspServices _lspServices;

    protected RequestDispatcher(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public abstract ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> GetRequestHandlers();

    public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
        string methodName,
        TRequestType request,
        LSP.ClientCapabilities clientCapabilities,
        IRequestExecutionQueue<RequestContextType> queue,
        CancellationToken cancellationToken)
    {
        // Get the handler matching the requested method.
        var requestHandlerMetadata = new RequestHandlerMetadata(methodName, typeof(TRequestType), typeof(TResponseType));

        var requestHandlers = GetRequestHandlers();
        var handler = requestHandlers[requestHandlerMetadata].Value;

        var mutatesSolutionState = handler.MutatesSolutionState;
        var requiresLspSolution = handler.RequiresLSPSolution;

        var strongHandler = (IRequestHandler<TRequestType, TResponseType, RequestContextType>?)handler;
        if (strongHandler is null)
        {
            throw new ArgumentOutOfRangeException(string.Format("Request handler not found for method {0}", methodName));
        }

        var result = await ExecuteRequestAsync(queue, mutatesSolutionState, requiresLspSolution, strongHandler, request, clientCapabilities, methodName, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected virtual Task<TResponseType> ExecuteRequestAsync<TRequestType, TResponseType>(
        IRequestExecutionQueue<RequestContextType> queue,
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        LSP.ClientCapabilities clientCapabilities,
        string methodName,
        CancellationToken cancellationToken)
    {
        return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, clientCapabilities, methodName, cancellationToken);
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }
}
