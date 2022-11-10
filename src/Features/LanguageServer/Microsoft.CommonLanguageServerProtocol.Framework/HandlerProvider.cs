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

        if (lspServices.SupportsGetRegisteredServices())
        {
            var requestHandlerTypes = lspServices.GetRegisteredServices().Where(type => IsTypeRequestHandler(type));

            foreach (var handlerType in requestHandlerTypes)
            {
                var (requestType, responseType, requestContext) = ConvertHandlerTypeToRequestResponseTypes(handlerType);
                var method = GetRequestHandlerMethod(handlerType);

                // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                // and then lookup the correct handler for the specified method.

                CheckForDuplicates(method, handlerType, requestHandlerDictionary);

                requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestType, responseType), new Lazy<IMethodHandler>(() =>
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

        var handlers = lspServices.GetRequiredServices<IMethodHandler>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var (requestType, responseType, requestContext) = ConvertHandlerTypeToRequestResponseTypes(handlerType);
            var method = GetRequestHandlerMethod(handlerType);
            CheckForDuplicates(method, handlerType, requestHandlerDictionary);

            requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestType, responseType), new Lazy<IMethodHandler>(() => handler));
        }

        VerifyHandlers(requestHandlerDictionary.Keys);

        return requestHandlerDictionary.ToImmutable();

        static void CheckForDuplicates(string methodName, Type handlerType, ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>.Builder handlerDict)
        {
            var dupHandlers = handlerDict.Where(kvp => string.Equals(kvp.Key.MethodName, methodName, StringComparison.InvariantCulture));
            if (dupHandlers.Any())
            {
                throw new InvalidOperationException($"Method {methodName} was implemented by both {handlerType} and {dupHandlers.First().Key}");
            }
        }

        static string GetRequestHandlerMethod(Type handlerType)
        {
            // Get the LSP method name from the handler's method name attribute.
            var methodAttribute = GetMethodAttribute(handlerType);
            if (methodAttribute is null)
            {
                throw new InvalidOperationException($"{handlerType.FullName} is missing {nameof(LanguageServerEndpointAttribute)}");
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
            return type.GetInterfaces().Contains(typeof(IMethodHandler));
        }

        static void VerifyHandlers(IEnumerable<RequestHandlerMetadata> requestHandlerKeys)
        {
            var missingMethods = requestHandlerKeys.Where(meta => RequiredMethods.All(method => method == meta.MethodName));
            if (missingMethods.Count() > 0)
            {
                throw new InvalidOperationException($"Language Server is missing required methods {string.Join(",", missingMethods)}");
            }
        }
    }

    private static readonly IReadOnlyList<string> RequiredMethods = new List<string> { "initialize", "initialized", "shutdown", "exit" };

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
