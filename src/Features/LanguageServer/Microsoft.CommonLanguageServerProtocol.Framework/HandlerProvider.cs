// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <inheritdoc/>
internal class HandlerProvider : IHandlerProvider
{
    private readonly ILspServices _lspServices;
    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>? _requestHandlers;

    public HandlerProvider(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    /// <summary>
    /// Get the MethodHandler for a particular request.
    /// </summary>
    /// <param name="method">The method name being made.</param>
    /// <param name="requestType">The requestType for this method.</param>
    /// <param name="responseType">The responseType for this method.</param>
    /// <returns>The handler for this request.</returns>
    public IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType);

        var requestHandlers = GetRequestHandlers();
        if (!requestHandlers.TryGetValue(requestHandlerMetadata, out var lazyHandler))
        {
            throw new InvalidOperationException($"Missing handler for {requestHandlerMetadata.MethodName}");
        }
        var handler = lazyHandler.Value;

        return handler;
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }

    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> GetRequestHandlers()
    {
        if (_requestHandlers is null)
        {
            _requestHandlers = CreateMethodToHandlerMap(_lspServices);
        }

        return _requestHandlers;
    }

    private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> CreateMethodToHandlerMap(ILspServices lspServices)
    {
        var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IMethodHandler>>();

        var methodHash = new HashSet<string>();

        if (lspServices.SupportsGetRegisteredServices())
        {
            var requestHandlerTypes = lspServices.GetRegisteredServices().Where(type => IsTypeRequestHandler(type));

            foreach (var handlerType in requestHandlerTypes)
            {
                var requestResponseTypes = ConvertHandlerTypeToRequestResponseTypes(handlerType);
                foreach (var requestResponseType in requestResponseTypes)
                {
                    var method = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.

                    CheckForDuplicates(method, methodHash);

                    requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType), new Lazy<IMethodHandler>(() =>
                        {
                            var lspService = lspServices.TryGetService(handlerType);
                            if (lspService is null)
                            {
                                throw new InvalidOperationException($"{handlerType} could not be retrieved from service");
                            }

                            return (IMethodHandler)lspService;
                        }));
                }
            }
        }

        var handlers = lspServices.GetRequiredServices<IMethodHandler>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var requestResponseTypes = ConvertHandlerTypeToRequestResponseTypes(handlerType);
            foreach (var requestResponseType in requestResponseTypes)
            {
                var method = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);
                CheckForDuplicates(method, methodHash);

                requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType), new Lazy<IMethodHandler>(() => handler));
            }
        }

        VerifyHandlers(requestHandlerDictionary.Keys);

        return requestHandlerDictionary.ToImmutable();

        static void CheckForDuplicates(string methodName, HashSet<string> existingMethods)
        {
            if (!existingMethods.Add(methodName))
            {
                throw new InvalidOperationException($"Method {methodName} was implemented more than once.");
            }
        }

        static string GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
        {
            // Get the LSP method name from the handler's method name attribute.
            var methodAttribute = GetMethodAttributeFromClassOrInterface(handlerType);
            if (methodAttribute is null)
            {
                methodAttribute = GetMethodAttributeFromHandlerMethod(handlerType, requestType, contextType, responseType);

                if (methodAttribute is null)
                {
                    throw new InvalidOperationException($"{handlerType.FullName} is missing {nameof(LanguageServerEndpointAttribute)}");
                }
            }

            return methodAttribute.Method;

            static LanguageServerEndpointAttribute? GetMethodAttributeFromHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
            {
                var methodInfo = (requestType != null, responseType != null) switch
                {
                    (true, true) => handlerType.GetMethod(nameof(IRequestHandler<object, object, object>.HandleRequestAsync), new Type[] { requestType!, contextType, typeof(CancellationToken) }),
                    (false, true) => handlerType.GetMethod(nameof(IRequestHandler<object, object>.HandleRequestAsync), new Type[] { contextType, typeof(CancellationToken) }),
                    (true, false) => handlerType.GetMethod(nameof(INotificationHandler<object, object>.HandleNotificationAsync), new Type[] { requestType!, contextType, typeof(CancellationToken) }),
                    (false, false) => handlerType.GetMethod(nameof(INotificationHandler<object>.HandleNotificationAsync), new Type[] { contextType, typeof(CancellationToken) })
                };

                if (methodInfo is null)
                {
                    throw new InvalidOperationException("Somehow we are missing the method for our registered handler");
                }

                return methodInfo.GetCustomAttribute<LanguageServerEndpointAttribute>();
            }

            static LanguageServerEndpointAttribute? GetMethodAttributeFromClassOrInterface(Type type)
            {
                var attribute = Attribute.GetCustomAttribute(type, typeof(LanguageServerEndpointAttribute)) as LanguageServerEndpointAttribute;
                if (attribute is null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        attribute = GetMethodAttributeFromClassOrInterface(@interface);
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
            return type.GetInterfaces().Contains(typeof(IMethodHandler));
        }

        static void VerifyHandlers(IEnumerable<RequestHandlerMetadata> requestHandlerKeys)
        {
            var missingMethods = requestHandlerKeys.Where(meta => RequiredMethods.All(method => method == meta.MethodName));
            if (missingMethods.Any())
            {
                throw new InvalidOperationException($"Language Server is missing required methods {string.Join(",", missingMethods)}");
            }
        }
    }

    private static readonly IReadOnlyList<string> RequiredMethods = new List<string> { "initialize", "initialized", "shutdown", "exit" };

    private record HandlerTypes(Type? RequestType, Type? ResponseType, Type RequestContext);

    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    private static List<HandlerTypes> ConvertHandlerTypeToRequestResponseTypes(Type handlerType)
    {
        var genericInterfaces = handlerType.GetInterfaces().Where(i => i.IsGenericType);
        var requestHandlerGenericTypes = GetGenericTypes(genericInterfaces, typeof(IRequestHandler<,,>));
        var parameterlessNotificationHandlerGenericTypes = GetGenericTypes(genericInterfaces, typeof(INotificationHandler<>));
        var notificationHandlerGenericTypes = GetGenericTypes(genericInterfaces, typeof(INotificationHandler<,>));

        var handlerList = new List<HandlerTypes>();

        foreach (var requestHandlerGenericType in requestHandlerGenericTypes)
        {
            var genericArguments = requestHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 3)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly three generic arguments");
            }

            handlerList.Add(new HandlerTypes(RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContext: genericArguments[2]));
        }

        foreach (var parameterlessNotificationHandlerGenericType in parameterlessNotificationHandlerGenericTypes)
        {
            var genericArguments = parameterlessNotificationHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 1)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly 1 generic argument");
            }

            handlerList.Add(new HandlerTypes(RequestType: null, ResponseType: null, RequestContext: genericArguments[0]));
        }

        foreach (var notificationHandlerGenericType in notificationHandlerGenericTypes)
        {
            var genericArguments = notificationHandlerGenericType.GetGenericArguments();

            if (genericArguments.Length != 2)
            {
                throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not have exactly 2 generic arguments");
            }

            handlerList.Add(new HandlerTypes(RequestType: genericArguments[0], ResponseType: null, RequestContext: genericArguments[1]));
        }

        if (!handlerList.Any())
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {typeof(IRequestHandler<,,>).Name}, {typeof(INotificationHandler<>).Name} or {typeof(INotificationHandler<,>).Name}");
        }

        return handlerList;

        static IEnumerable<Type> GetGenericTypes(IEnumerable<Type> genericInterfaces, Type methodHandlerType)
        {
            return genericInterfaces.Where(i => i.GetGenericTypeDefinition() == methodHandlerType);
        }
    }
}
