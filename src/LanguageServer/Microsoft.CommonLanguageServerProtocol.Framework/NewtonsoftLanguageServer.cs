// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Basic implementation of <see cref="AbstractLanguageServer{TRequestContext}"/> using Newtonsoft for serialization.
/// </summary>
internal abstract class NewtonsoftLanguageServer<TRequestContext>(
    JsonRpc jsonRpc, JsonSerializer jsonSerializer, ILspLogger logger, AbstractTypeRefResolver? typeRefResolver = null)
    : AbstractLanguageServer<TRequestContext>(jsonRpc, logger, typeRefResolver)
{
    private readonly JsonSerializer _jsonSerializer = jsonSerializer;

    protected override DelegatingEntryPoint CreateDelegatingEntryPoint(string method, IGrouping<string, RequestHandlerMetadata> handlersForMethod)
    {
        return new NewtonsoftDelegatingEntryPoint(method, handlersForMethod, this);
    }

    protected virtual string GetLanguageForRequest(string methodName, JToken? parameters)
    {
        Logger.LogInformation($"Using default language handler for {methodName}");
        return LanguageServerConstants.DefaultLanguageName;
    }

    private class NewtonsoftDelegatingEntryPoint(
        string method,
        IGrouping<string, RequestHandlerMetadata> handlersForMethod,
        NewtonsoftLanguageServer<TRequestContext> target) : DelegatingEntryPoint(method, target.TypeRefResolver, handlersForMethod)
    {
        private static readonly MethodInfo s_entryPoint = typeof(NewtonsoftDelegatingEntryPoint).GetMethod(nameof(NewtonsoftDelegatingEntryPoint.ExecuteRequestAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override MethodInfo GetEntryPoint(bool hasParameter)
        {
            return s_entryPoint;
        }

        /// <summary>
        /// StreamJsonRpc entry point for all handler methods.
        /// The optional parameters allow StreamJsonRpc to call into the same method for any kind of request / notification (with any number of params or response).
        /// </summary>
        private async Task<JToken?> ExecuteRequestAsync(JToken? request = null, CancellationToken cancellationToken = default)
        {
            var queue = target.GetRequestExecutionQueue();
            var lspServices = target.GetLspServices();

            // Retrieve the language of the request so we know how to deserialize it.
            var language = target.GetLanguageForRequest(_method, request);

            // Find the correct request and response types for the given request and language.
            var requestInfo = GetMethodInfo(language);

            // Deserialize the request parameters (if any).
            var requestObject = DeserializeRequest(request, requestInfo.Metadata, target._jsonSerializer);

            var result = await InvokeAsync(requestInfo.MethodInfo, queue, requestObject, language, lspServices, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                return null;
            }

            return JToken.FromObject(result, target._jsonSerializer);
        }

        private object DeserializeRequest(JToken? request, RequestHandlerMetadata metadata, JsonSerializer jsonSerializer)
        {
            var requestTypeRef = metadata.RequestTypeRef;

            if (request is null)
            {
                if (requestTypeRef is not null)
                {
                    throw new InvalidOperationException($"Handler {metadata.HandlerDescription} requires request parameters but received none");
                }

                return NoValue.Instance;
            }

            // request is not null
            if (requestTypeRef is null)
            {
                throw new InvalidOperationException($"Handler {metadata.HandlerDescription} does not accept parameters, but received some.");
            }

            var requestType = _typeRefResolver.Resolve(requestTypeRef)
                ?? throw new InvalidOperationException($"Could not resolve type: '{requestTypeRef}'");

            return request.ToObject(requestType, jsonSerializer)
                ?? throw new InvalidOperationException($"Unable to deserialize {request} into {requestTypeRef} for {metadata.HandlerDescription}");
        }
    }
}
