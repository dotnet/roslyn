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

    public override TRequest DeserializeRequest<TRequest>(object? serializedRequest, RequestHandlerMetadata metadata)
    {
        var requestTypeRef = metadata.RequestTypeRef;

        if (serializedRequest is null)
        {
            if (requestTypeRef is not null)
            {
                throw new InvalidOperationException($"Handler {metadata.HandlerDescription} requires request parameters but received none");
            }

            // We checked that TRequest is typeof(NoValue).
            return (TRequest)(object)NoValue.Instance;
        }

        // request is not null
        if (requestTypeRef is null)
        {
            throw new InvalidOperationException($"Handler {metadata.HandlerDescription} does not accept parameters, but received some.");
        }

        var request = (JToken)serializedRequest;

        return request.ToObject<TRequest>(_jsonSerializer)
            ?? throw new InvalidOperationException($"Unable to deserialize {request} into {requestTypeRef} for {metadata.HandlerDescription}");
    }

    protected override DelegatingEntryPoint CreateDelegatingEntryPoint(string method)
    {
        return new NewtonsoftDelegatingEntryPoint(method, this);
    }

    private class NewtonsoftDelegatingEntryPoint(
        string method,
        NewtonsoftLanguageServer<TRequestContext> target) : DelegatingEntryPoint(method)
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

            var result = await InvokeAsync(queue, request, lspServices, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                return null;
            }

            return JToken.FromObject(result, target._jsonSerializer);
        }
    }
}
