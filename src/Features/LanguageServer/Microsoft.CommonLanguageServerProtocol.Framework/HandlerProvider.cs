// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <inheritdoc/>
internal class HandlerProvider(ILspServices lspServices) : AbstractHandlerProvider
{
    private readonly ILspServices _lspServices = lspServices;
    private FrozenDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>? _requestHandlers;

    public IMethodHandler GetMethodHandler(string method, TypeRef? requestTypeRef, TypeRef? responseTypeRef)
        => GetMethodHandler(method, requestTypeRef, responseTypeRef, LanguageServerConstants.DefaultLanguageName);

    public override IMethodHandler GetMethodHandler(string method, TypeRef? requestTypeRef, TypeRef? responseTypeRef, string language)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestTypeRef, responseTypeRef, language);
        var defaultHandlerMetadata = new RequestHandlerMetadata(method, requestTypeRef, responseTypeRef, LanguageServerConstants.DefaultLanguageName);

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
        return requestHandlers.Keys;
    }

    private FrozenDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> GetRequestHandlers()
        => _requestHandlers ??= CreateMethodToHandlerMap(_lspServices);

    private static FrozenDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> CreateMethodToHandlerMap(ILspServices lspServices)
    {
        var builder = new Dictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>();

        var methodHash = new HashSet<(string methodName, string language)>();

        // First, see if the ILspServices provides a special path for retrieving method handlers.
        if (lspServices is IMethodHandlerProvider methodHandlerProvider)
        {
            foreach (var (handlerType, descriptors) in methodHandlerProvider.GetMethodHandlers())
            {
                foreach (var (methodName, language, requestTypeRef, responseTypeRef, _) in descriptors)
                {
                    CheckForDuplicates(methodName, language, methodHash);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.
                    builder.Add(
                        new RequestHandlerMetadata(methodName, requestTypeRef, responseTypeRef, language),
                        new Lazy<IMethodHandler>(() =>
                        {
                            if (!lspServices.TryGetService(handlerType.GetResolvedType(), out var lspService))
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
            var handlerDetails = HandlerReflection.GetHandlerDetails(handlerType);

            foreach (var (requestType, responseType, requestContextType) in handlerDetails)
            {
                var (method, languages) = HandlerReflection.GetRequestHandlerMethod(handlerType, requestType, requestContextType, responseType);

                foreach (var language in languages)
                {
                    CheckForDuplicates(method, language, methodHash);

                    builder.Add(
                        new RequestHandlerMetadata(method, requestType, responseType, language),
                        new Lazy<IMethodHandler>(() => handler));
                }
            }
        }

        VerifyHandlers(builder.Keys);

        return builder.ToFrozenDictionary();

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
