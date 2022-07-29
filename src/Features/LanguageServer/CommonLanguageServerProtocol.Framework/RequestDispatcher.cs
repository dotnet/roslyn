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
public class RequestDispatcher<RequestContextType> : IRequestDispatcher<RequestContextType> where RequestContextType : IRequestContext
{
    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>>? _requestHandlers;
    protected ILspServices _lspServices;

    public RequestDispatcher(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public virtual ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> GetRequestHandlers()
    {
        if (_requestHandlers is null)
        {
            _requestHandlers = CreateMethodToHandlerMap(_lspServices);
        }

        return _requestHandlers;
    }

    public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
        string methodName,
        TRequestType request,
        RequestExecutionQueue<RequestContextType> queue,
        CancellationToken cancellationToken)
    {
        // Get the handler matching the requested method.
        var handler = GetRequestHandler(methodName, typeof(TRequestType), typeof(TResponseType));

        var mutatesSolutionState = handler.MutatesSolutionState;
        var requiresLspSolution = handler.RequiresLSPSolution;

        var strongHandler = (IRequestHandler<TRequestType, TResponseType, RequestContextType>?)handler;
        if (strongHandler is null)
        {
            throw new ArgumentOutOfRangeException(string.Format("Request handler not found for method {0}", methodName));
        }

        var result = await ExecuteRequestAsync(queue, mutatesSolutionState, requiresLspSolution, strongHandler, request, methodName, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task ExecuteNotificationAsync<TRequestType>(string methodName, TRequestType request, RequestExecutionQueue<RequestContextType> queue, CancellationToken cancellationToken)
    {
        var handler = GetRequestHandler(methodName, typeof(TRequestType), responseType: null);

        var strongHandler = (INotificationHandler<TRequestType, RequestContextType>?)handler;
        if (strongHandler is null)
        {
            throw new ArgumentOutOfRangeException(string.Format("Request handler not found for method {0}", methodName));
        }

        await ExecuteNotificationAsync(queue, handler.MutatesSolutionState, handler.RequiresLSPSolution, strongHandler, request, methodName, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task ExecuteNotificationAsync<TRequestType>(
        RequestExecutionQueue<RequestContextType> queue,
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<TRequestType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        CancellationToken cancellationToken)
    {
        return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, methodName, cancellationToken);
    }

    public async Task ExecuteNotificationAsync(string methodName, RequestExecutionQueue<RequestContextType> queue, CancellationToken cancellationToken)
    {
        var handler = GetRequestHandler(methodName, requestType: null, responseType: null);

        var strongHandler = (INotificationHandler<RequestContextType>?)handler;
        if (strongHandler is null)
        {
            throw new ArgumentOutOfRangeException(string.Format("Request handler not found for method {0}", methodName));
        }

        await ExecuteNotificationAsync(queue, handler.MutatesSolutionState, handler.RequiresLSPSolution, strongHandler, methodName, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task ExecuteNotificationAsync(RequestExecutionQueue<RequestContextType> queue, bool mutatesSolutionState, bool requiresLSPSolution, INotificationHandler<RequestContextType> handler, string methodName, CancellationToken cancellationToken)
    {
        return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, methodName, cancellationToken);
    }

    private IRequestHandler GetRequestHandler(string method, Type? requestType, Type? responseType)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType);

        var requestHandlers = GetRequestHandlers();
        var handler = requestHandlers[requestHandlerMetadata].Value;

        return handler;
    }

    protected virtual Task<TResponseType> ExecuteRequestAsync<TRequestType, TResponseType>(
        RequestExecutionQueue<RequestContextType> queue,
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        CancellationToken cancellationToken)
    {
        return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, methodName, cancellationToken);
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }

    private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> CreateMethodToHandlerMap(ILspServices lspServices)
    {
        var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IRequestHandler>>();

        var requestHandlerTypes = lspServices.GetRegisteredServices().Where(type => IsTypeRequestHandler(type));

        foreach (var handlerType in requestHandlerTypes)
        {
            var (requestType, responseType, requestContext) = ConvertHandlerTypeToRequestResponseTypes(handlerType);
            var method = GetRequestHandlerMethod(handlerType);

            // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
            // and then lookup the correct handler for the specified method.
            requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestType, responseType), new Lazy<IRequestHandler>(() =>
            {
                if (!lspServices.TryGetService(handlerType, out var lspService))
                {
                    throw new InvalidOperationException($"{handlerType} could not be retrieved from service");
                }

                return (IRequestHandler)lspService;
            }));
        }

        return requestHandlerDictionary.ToImmutable();

        static string GetRequestHandlerMethod(Type handlerType)
        {
            // Get the LSP method name from the handler's method name attribute.
            var methodAttribute = GetMethodAttribute(handlerType);
            if (methodAttribute is null)
            {
                throw new InvalidOperationException($"{handlerType.FullName} is missing Method attribute");
            }
            return methodAttribute.Method;

            static LanguageServerEndpointAttribute? GetMethodAttribute(Type type)
            {
                var attribute = Attribute.GetCustomAttribute(type, typeof(LanguageServerEndpointAttribute)) as LanguageServerEndpointAttribute;
                if (attribute is null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        attribute = GetMethodAttribute(@interface);
                        if (attribute is not null)
                        {
                            break;
                        }
                    }
                }

                return attribute;
            }
        }

        static bool IsTypeRequestHandler(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IRequestHandler));
        }
    }

    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    private static (Type? requestType, Type? responseType, Type requestContext) ConvertHandlerTypeToRequestResponseTypes(Type handlerType)
    {
        var requestHandlerGenericType = handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,,>)).SingleOrDefault();
        var parameterlessNotificationHandlerGenericType = handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)).SingleOrDefault();
        var notificationHandlerGenericType = handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<,>)).SingleOrDefault();

        Type? requestType;
        Type? responseType;
        Type requestContext;
        if (requestHandlerGenericType is not null)
        {
            var genericArguments = requestHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 3)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly three generic arguments");
            }

            requestType = genericArguments[0];
            responseType = genericArguments[1];
            requestContext = genericArguments[2];
        }
        else if (parameterlessNotificationHandlerGenericType is not null)
        {
            var genericArguments = parameterlessNotificationHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 1)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly 1 generic argument");
            }

            requestType = null;
            responseType = null;
            requestContext = genericArguments[0];
        }
        else if (notificationHandlerGenericType is not null)
        {
            var genericArguments = notificationHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 2)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly 2 generic arguments");
            }

            requestType = genericArguments[0];
            responseType = null;
            requestContext = genericArguments[1];
        }
        else
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {typeof(IRequestHandler<,,>).Name}, {typeof(INotificationHandler<>).Name} or {typeof(INotificationHandler<,>).Name}");
        }

        return (requestType, responseType, requestContext);
    }
}
