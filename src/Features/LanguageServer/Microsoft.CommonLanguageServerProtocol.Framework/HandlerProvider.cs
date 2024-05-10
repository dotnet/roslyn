// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <inheritdoc/>
internal class HandlerProvider : AbstractHandlerProvider
{
    private readonly ILspServices _lspServices;
    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>? _requestHandlers;

    public HandlerProvider(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType)
        => GetMethodHandler(method, requestType, responseType, LanguageServerConstants.DefaultLanguageName);

    public override IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType, string language)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType, language);
        var defaultHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType, LanguageServerConstants.DefaultLanguageName);

        var requestHandlers = GetRequestHandlers();
        if (!requestHandlers.TryGetValue(requestHandlerMetadata, out var lazyHandler) &&
            !requestHandlers.TryGetValue(defaultHandlerMetadata, out lazyHandler))
        {
            throw new InvalidOperationException($"Missing handler for {requestHandlerMetadata.MethodName}");
        }

        return lazyHandler.Value;
    }

    public override ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }

    private ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> GetRequestHandlers()
        => _requestHandlers ??= CreateMethodToHandlerMap(_lspServices);

    private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> CreateMethodToHandlerMap(ILspServices lspServices)
    {
        var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IMethodHandler>>();

        var methodHash = new HashSet<(string methodName, string language)>();

        // First, see if the ILspServices provides a special path for retrieving method handlers.
        if (lspServices is IMethodHandlerProvider methodHandlerProvider)
        {
            var requestHandlerTypes = methodHandlerProvider.GetMethodHandlers();

            foreach (var handlerType in requestHandlerTypes)
            {
                var requestResponseTypes = HandlerTypes.ConvertHandlerTypeToRequestResponseTypes(handlerType);
                foreach (var requestResponseType in requestResponseTypes)
                {
                    var (method, languages) = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);

                    foreach (var language in languages)
                    {
                        CheckForDuplicates(method, language, methodHash);

                        // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                        // and then lookup the correct handler for the specified method.
                        requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType, language), new Lazy<IMethodHandler>(() =>
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
        }

        var handlers = lspServices.GetRequiredServices<IMethodHandler>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var requestResponseTypes = HandlerTypes.ConvertHandlerTypeToRequestResponseTypes(handlerType);
            foreach (var requestResponseType in requestResponseTypes)
            {
                var (method, languages) = GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContext, requestResponseType.ResponseType);

                foreach (var language in languages)
                {
                    CheckForDuplicates(method, language, methodHash);

                    requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestResponseType.RequestType, requestResponseType.ResponseType, language), new Lazy<IMethodHandler>(() => handler));
                }
            }
        }

        VerifyHandlers(requestHandlerDictionary.Keys);

        return requestHandlerDictionary.ToImmutable();

        static void CheckForDuplicates(string methodName, string language, HashSet<(string methodName, string language)> existingMethods)
        {
            if (!existingMethods.Add((methodName, language)))
            {
                throw new InvalidOperationException($"Method {methodName} was implemented more than once.");
            }
        }

        static (string name, IEnumerable<string> languages) GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
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

            return (methodAttribute.Method, methodAttribute.Languages);

            static LanguageServerEndpointAttribute? GetMethodAttributeFromHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
            {
                const string handleRequestName = nameof(IRequestHandler<object, object, object>.HandleRequestAsync);
                const string handleNotificationName = nameof(INotificationHandler<object, object>.HandleNotificationAsync);

                foreach (var methodInfo in handlerType.GetRuntimeMethods())
                {
                    if (MethodInfoMatches(methodInfo))
                        return methodInfo.GetCustomAttribute<LanguageServerEndpointAttribute>();
                }

                throw new InvalidOperationException("Somehow we are missing the method for our registered handler");

                bool MethodInfoMatches(MethodInfo methodInfo)
                {
                    switch (requestType != null, responseType != null)
                    {
                        case (true, true):
                            return (methodInfo.Name == handleRequestName || methodInfo.Name.EndsWith("." + handleRequestName)) &&
                                TypesMatch(methodInfo, [requestType!, contextType, typeof(CancellationToken)]);
                        case (false, true):
                            return (methodInfo.Name == handleRequestName || methodInfo.Name.EndsWith("." + handleRequestName)) &&
                                TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);
                        case (true, false):
                            return (methodInfo.Name == handleNotificationName || methodInfo.Name.EndsWith("." + handleNotificationName)) &&
                                TypesMatch(methodInfo, [requestType!, contextType, typeof(CancellationToken)]);
                        case (false, false):
                            return (methodInfo.Name == handleNotificationName || methodInfo.Name.EndsWith("." + handleNotificationName)) &&
                                TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);
                    }
                }

                bool TypesMatch(MethodInfo methodInfo, Type[] types)
                {
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length != types.Length)
                        return false;

                    for (int i = 0, n = parameters.Length; i < n; i++)
                    {
                        if (!Equals(types[i], parameters[i].ParameterType))
                            return false;
                    }

                    return true;
                }
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
}
