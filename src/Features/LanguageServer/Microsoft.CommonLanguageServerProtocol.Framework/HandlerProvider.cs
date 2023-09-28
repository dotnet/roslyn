// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <inheritdoc/>
internal class HandlerProvider : IHandlerProvider
{
    private readonly ILspServices _lspServices;
    private ImmutableDictionary<RequestHandlerMetadata, List<Lazy<IMethodHandler, string?>>>? _requestHandlers;

    public HandlerProvider(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    /// <summary>
    /// Get the <see cref="IMethodHandler"/>s for a particular request.
    /// </summary>
    /// <param name="method">The method name being made.</param>
    /// <param name="requestType">The requestType for this method.</param>
    /// <param name="responseType">The responseType for this method.</param>
    /// <returns>The handler for this request.</returns>
    public ImmutableArray<Lazy<IMethodHandler, string?>> GetMethodHandlers(string method, Type? requestType, Type? responseType)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType);

        var requestHandlers = GetRequestHandlers();
        if (!requestHandlers.TryGetValue(requestHandlerMetadata, out var lazyHandlers))
        {
            throw new InvalidOperationException($"Missing handler for {requestHandlerMetadata.MethodName}");
        }

        return lazyHandlers.ToImmutableArray();
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }

    private ImmutableDictionary<RequestHandlerMetadata, List<Lazy<IMethodHandler, string?>>> GetRequestHandlers()
        => _requestHandlers ??= CreateMethodToHandlerMap(_lspServices);

    private static ImmutableDictionary<RequestHandlerMetadata, List<Lazy<IMethodHandler, string?>>> CreateMethodToHandlerMap(ILspServices lspServices)
    {
        var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, List<Lazy<IMethodHandler, string?>>>();

        var methodHash = new HashSet<(string, string?)>();

        if (lspServices.SupportsGetRegisteredServices())
        {
            var requestHandlerTypes = lspServices.GetRegisteredServices().Where(type => typeof(IMethodHandler).IsAssignableFrom(type));

            foreach (var handlerType in requestHandlerTypes)
            {
                var requestResponseTypes = ConvertHandlerTypeToRequestResponseTypes(handlerType);
                foreach (var requestResponseType in requestResponseTypes)
                {
                    var method = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.

                    CheckForDuplicates(method, methodHash);

                    var lazyHandler = new Lazy<IMethodHandler, string?>(() =>
                    {
                        var lspService = lspServices.TryGetService(handlerType);
                        if (lspService is null)
                        {
                            throw new InvalidOperationException($"{handlerType} could not be retrieved from service");
                        }

                        return (IMethodHandler)lspService;
                    }, method.Language);

                    var requestHandlerMetadata = new RequestHandlerMetadata(method.Name, requestResponseType.RequestType, requestResponseType.ResponseType);
                    if (!requestHandlerDictionary.TryGetValue(requestHandlerMetadata, out var methodHandlers))
                    {
                        methodHandlers = new List<Lazy<IMethodHandler, string?>>();
                        requestHandlerDictionary.Add(requestHandlerMetadata, methodHandlers);
                    }
                    methodHandlers.Add(lazyHandler);
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

                var requestHandlerMetadata = new RequestHandlerMetadata(method.Name, requestResponseType.RequestType, requestResponseType.ResponseType);
                if (!requestHandlerDictionary.TryGetValue(requestHandlerMetadata, out var methodHandlers))
                {
                    methodHandlers = new List<Lazy<IMethodHandler, string?>>();
                    requestHandlerDictionary.Add(requestHandlerMetadata, methodHandlers);
                }
                methodHandlers.Add(new Lazy<IMethodHandler, string?>(() => handler, method.Language));
            }
        }

        VerifyHandlers(requestHandlerDictionary.Keys);

        return requestHandlerDictionary.ToImmutable();

        static void CheckForDuplicates((string, string?) method, HashSet<(string, string?)> existingMethods)
        {
            if (!existingMethods.Add(method))
            {
                throw new InvalidOperationException($"Method {method.Item1} was implemented more than once.");
            }
        }

        static (string Name, string? Language) GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
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

            return (methodAttribute.Method, methodAttribute.Language);

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
        var handlerList = new List<HandlerTypes>();

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();

            HandlerTypes types;
            if (genericDefinition == typeof(IRequestHandler<,,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContext: genericArguments[2]);
            }
            else if (genericDefinition == typeof(IRequestHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: genericArguments[0], RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: null, RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: null, RequestContext: genericArguments[0]);
            }
            else
            {
                continue;
            }

            handlerList.Add(types);
        }

        if (handlerList.Count == 0)
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {nameof(IMethodHandler)}");
        }

        return handlerList;
    }
}
