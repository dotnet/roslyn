// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

    public IMethodHandler GetMethodHandler(string method, LazyType? requestType, LazyType? responseType)
        => GetMethodHandler(method, requestType, responseType, LanguageServerConstants.DefaultLanguageName);

    public override IMethodHandler GetMethodHandler(string method, LazyType? requestType, LazyType? responseType, string language)
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
            foreach (var (handlerType, descriptors) in methodHandlerProvider.GetMethodHandlers())
            {
                foreach (var descriptor in descriptors)
                {
                    CheckForDuplicates(descriptor.MethodName, descriptor.Language, methodHash);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.
                    requestHandlerDictionary.Add(
                        new RequestHandlerMetadata(descriptor.MethodName, LazyType.FromOrNull(descriptor.RequestTypeName), LazyType.FromOrNull(descriptor.ResponseTypeName), descriptor.Language),
                        new Lazy<IMethodHandler>(() =>
                        {
                            var lspService = lspServices.TryGetService(handlerType.Value);
                            if (lspService is null)
                            {
                                throw new InvalidOperationException($"{handlerType} could not be retrieved from service");
                            }

                            return (IMethodHandler)lspService;
                        }));
                }
            }
        }

        // No fast path was provided, so we must realize all of of the services.
        var handlers = lspServices.GetRequiredServices<IMethodHandler>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var handlerTypes = HandlerTypes.ConvertHandlerTypeToRequestResponseTypes(handlerType);
            foreach (var requestResponseType in handlerTypes)
            {
                var (method, languages) = HandlerReflection.GetRequestHandlerMethod(handlerType, requestResponseType.RequestType, requestResponseType.RequestContextType, requestResponseType.ResponseType);

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
