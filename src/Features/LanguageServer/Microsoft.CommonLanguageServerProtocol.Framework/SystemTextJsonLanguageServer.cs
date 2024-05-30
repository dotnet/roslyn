// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal abstract class SystemTextJsonLanguageServer<TRequestContext>(
    JsonRpc jsonRpc, JsonSerializerOptions options, ILspLogger logger, AbstractTypeRefResolver? typeRefResolver = null)
    : AbstractLanguageServer<TRequestContext>(jsonRpc, logger, typeRefResolver)
{
    /// <summary>
    /// JsonSerializer options used by streamjsonrpc (and for serializing / deserializing the requests to streamjsonrpc).
    /// These options are specifically from the <see cref="StreamJsonRpc.SystemTextJsonFormatter"/> that added the exotic type converters.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = options;

    protected override DelegatingEntryPoint CreateDelegatingEntryPoint(string method, IGrouping<string, RequestHandlerMetadata> handlersForMethod)
    {
        return new SystemTextJsonDelegatingEntryPoint(method, handlersForMethod, this);
    }

    protected virtual string GetLanguageForRequest(string methodName, JsonElement? parameters)
    {
        Logger.LogInformation($"Using default language handler for {methodName}");
        return LanguageServerConstants.DefaultLanguageName;
    }

    private sealed class SystemTextJsonDelegatingEntryPoint(
        string method,
        IGrouping<string, RequestHandlerMetadata> handlersForMethod,
        SystemTextJsonLanguageServer<TRequestContext> target) : DelegatingEntryPoint(method, target.TypeRefResolver, handlersForMethod)
    {
        private static readonly MethodInfo s_parameterlessEntryPoint = typeof(SystemTextJsonDelegatingEntryPoint).GetMethod(nameof(SystemTextJsonDelegatingEntryPoint.ExecuteRequest0Async), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_entryPoint = typeof(SystemTextJsonDelegatingEntryPoint).GetMethod(nameof(SystemTextJsonDelegatingEntryPoint.ExecuteRequestAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override MethodInfo GetEntryPoint(bool hasParameter)
        {
            return hasParameter ? s_entryPoint : s_parameterlessEntryPoint;
        }

        /// <summary>
        /// StreamJsonRpc entry point for handlers with no parameters.
        /// Unlike Newtonsoft, we have to differentiate instead of using default parameters.
        /// </summary>
        private Task<JsonElement?> ExecuteRequest0Async(CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync(null, cancellationToken);
        }

        /// <summary>
        /// StreamJsonRpc entry point for handlers with parameters (and any response) type.
        /// </summary>
        private async Task<JsonElement?> ExecuteRequestAsync(JsonElement? request, CancellationToken cancellationToken = default)
        {
            var queue = target.GetRequestExecutionQueue();
            var lspServices = target.GetLspServices();

            // Retrieve the language of the request so we know how to deserialize it.
            var language = target.GetLanguageForRequest(_method, request);

            // Find the correct request and response types for the given request and language.
            var requestInfo = GetMethodInfo(language);

            // Deserialize the request parameters (if any).
            var requestObject = DeserializeRequest(request, requestInfo.Metadata, target._jsonSerializerOptions);

            var result = await InvokeAsync(requestInfo.MethodInfo, queue, requestObject, language, lspServices, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                return null;
            }

            var serializedResult = JsonSerializer.SerializeToElement(result, target._jsonSerializerOptions);
            return serializedResult;
        }

        private object DeserializeRequest(JsonElement? request, RequestHandlerMetadata metadata, JsonSerializerOptions options)
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

            return JsonSerializer.Deserialize(request.Value, requestType, options)
                ?? throw new InvalidOperationException($"Unable to deserialize {request} into {requestTypeRef} for {metadata.HandlerDescription}");
        }
    }
}
