// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal abstract class SystemTextJsonLanguageServer<TRequestContext>(
    JsonRpc jsonRpc, JsonSerializerOptions options, AbstractTypeRefResolver? typeRefResolver = null)
    : AbstractLanguageServer<TRequestContext>(jsonRpc, typeRefResolver)
{
    /// <summary>
    /// JsonSerializer options used to deserialize incoming requests.
    /// <para>
    /// This is a copy of the options streamjsonrpc uses (which added the exotic type converters from
    /// <see cref="StreamJsonRpc.SystemTextJsonFormatter"/>) with our own <see cref="IProgress{T}"/> support
    /// layered on top - see <see cref="LspProgressConverterFactory"/> for why streamjsonrpc's cannot be used
    /// here. We deliberately copy rather than mutate the formatter's options, so that everything else on this
    /// connection (responses, notifications, and messages we originate) keeps streamjsonrpc's behavior.
    /// </para>
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = CreateRequestDeserializationOptions(options, jsonRpc);

    private static JsonSerializerOptions CreateRequestDeserializationOptions(JsonSerializerOptions formatterOptions, JsonRpc jsonRpc)
    {
        var requestOptions = new JsonSerializerOptions(formatterOptions);

        // Insert at the front - System.Text.Json uses the first converter in the list that can convert a
        // type, and the copied options already contain streamjsonrpc's IProgress<T> converter.
        requestOptions.Converters.Insert(0, new LspProgressConverterFactory(jsonRpc));
        return requestOptions;
    }

    public override TRequest DeserializeRequest<TRequest>(object? serializedRequest, RequestHandlerMetadata metadata)
    {
        if (serializedRequest is null)
        {
            if (metadata.RequestTypeRef is not null)
            {
                throw new InvalidOperationException($"Handler {metadata.HandlerDescription} requires request parameters but received none");
            }
            else
            {
                // We checked that TRequest is typeof(NoValue).
                return (TRequest)(object)NoValue.Instance;
            }
        }

        if (metadata.RequestTypeRef is null)
        {
            throw new InvalidOperationException($"Handler {metadata.HandlerDescription} does not accept parameters, but received some.");
        }

        var request = (JsonElement)serializedRequest;

        return JsonSerializer.Deserialize<TRequest>(request, _jsonSerializerOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize {request} into {typeof(TRequest)} for {metadata.HandlerDescription}");
    }

    protected override DelegatingEntryPoint CreateDelegatingEntryPoint(string method)
    {
        return new SystemTextJsonDelegatingEntryPoint(method, this);
    }

    private sealed class SystemTextJsonDelegatingEntryPoint(
        string method,
        SystemTextJsonLanguageServer<TRequestContext> target) : DelegatingEntryPoint(method)
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
        private Task<object?> ExecuteRequest0Async(CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync(null, cancellationToken);
        }

        /// <summary>
        /// StreamJsonRpc entry point for handlers with parameters (and any response) type.
        /// </summary>
        /// <remarks>
        /// Returns the handler's typed result directly as <see cref="object"/> rather than
        /// pre-serializing it into a <see cref="JsonElement"/>. StreamJsonRpc will serialize the
        /// result to the wire using <see cref="JsonSerializer"/> with the runtime type's converter,
        /// producing identical JSON. Returning <see cref="object"/> avoids a redundant
        /// serialize-then-reserialize round-trip that <see cref="JsonSerializer.SerializeToElement(object?, Type, JsonSerializerOptions?)"/>
        /// would otherwise cause (object → byte[] → JsonDocument → JsonElement → wire bytes).
        /// </remarks>
        private Task<object?> ExecuteRequestAsync(JsonElement? request, CancellationToken cancellationToken = default)
        {
            var queue = target.GetRequestExecutionQueue();
            var lspServices = target.GetLspServices();

            return InvokeAsync(queue, request, lspServices, cancellationToken);
        }
    }
}
