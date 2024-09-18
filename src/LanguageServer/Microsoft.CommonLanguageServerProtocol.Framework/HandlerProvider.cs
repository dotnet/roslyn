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
internal class HandlerProvider(ILspServices lspServices, AbstractTypeRefResolver typeRefResolver) : AbstractHandlerProvider
{
    private readonly ILspServices _lspServices = lspServices;
    private readonly AbstractTypeRefResolver _typeRefResolver = typeRefResolver;
    private FrozenDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>? _requestHandlers;

    public IMethodHandler GetMethodHandler(string method, TypeRef? requestTypeRef, TypeRef? responseTypeRef)
        => GetMethodHandler(method, requestTypeRef, responseTypeRef, LanguageServerConstants.DefaultLanguageName);

    public override IMethodHandler GetMethodHandler(string method, TypeRef? requestTypeRef, TypeRef? responseTypeRef, string language)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestTypeRef, responseTypeRef, language);

        var requestHandlers = GetRequestHandlers();
        if (!requestHandlers.TryGetValue(requestHandlerMetadata, out var lazyHandler))
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
        => _requestHandlers ??= CreateMethodToHandlerMap(_lspServices, _typeRefResolver);

    private static FrozenDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> CreateMethodToHandlerMap(ILspServices lspServices, AbstractTypeRefResolver typeRefResolver)
    {
        var builder = new Dictionary<RequestHandlerMetadata, Lazy<IMethodHandler>>();

        var methodHash = new HashSet<(string methodName, string language)>();

        // First, see if the ILspServices provides a special path for retrieving method handlers.
        if (lspServices is IMethodHandlerProvider methodHandlerProvider)
        {
            foreach (var (instance, handlerTypeRef, methods) in methodHandlerProvider.GetMethodHandlers())
            {
                foreach (var (methodName, language, requestTypeRef, responseTypeRef, _) in methods)
                {
                    CheckForDuplicates(methodName, language, methodHash);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.
                    builder.Add(
                        new RequestHandlerMetadata(methodName, requestTypeRef, responseTypeRef, language),
                        instance is not null
                            ? GetLazyHandlerFromInstance(instance)
                            : GetLazyHandlerFromTypeRef(lspServices, typeRefResolver, handlerTypeRef));
                }
            }
        }
        else
        {
            // No fast path was provided, so we must realize all of of the services.
            var handlers = lspServices.GetRequiredServices<IMethodHandler>();

            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType();
                var handlerDetails = MethodHandlerDetails.From(handlerType);

                foreach (var (methodName, language, requestTypeRef, responseTypeRef, _) in handlerDetails)
                {
                    CheckForDuplicates(methodName, language, methodHash);

                    builder.Add(
                        new RequestHandlerMetadata(methodName, requestTypeRef, responseTypeRef, language),
                        GetLazyHandlerFromInstance(handler));
                }
            }
        }

        VerifyHandlers(builder.Keys);

        return builder.ToFrozenDictionary();

        static Lazy<IMethodHandler> GetLazyHandlerFromInstance(IMethodHandler instance)
        {
            return new(() => instance);
        }

        static Lazy<IMethodHandler> GetLazyHandlerFromTypeRef(ILspServices lspServices, AbstractTypeRefResolver typeRefResolver, TypeRef handlerTypeRef)
        {
            return new(() =>
            {
                var handlerType = typeRefResolver.Resolve(handlerTypeRef)
                    ?? throw new InvalidOperationException($"Could not load type: '{handlerTypeRef}'");

                if (!lspServices.TryGetService(handlerType, out var lspService))
                {
                    throw new InvalidOperationException($"{handlerTypeRef} could not be retrieved from service");
                }

                return (IMethodHandler)lspService;
            });
        }

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
