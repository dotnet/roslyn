// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using StreamJsonRpc.Reflection;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Converts LSP progress tokens (for example <c>partialResultToken</c> and <c>workDoneToken</c>) into
/// <see cref="IProgress{T}"/> instances that report using the <c>$/progress</c> notification shape the
/// protocol requires.
/// </summary>
/// <remarks>
/// StreamJsonRpc has built-in <see cref="IProgress{T}"/> support, but it only emits the named
/// <c>{ token, value }</c> arguments LSP mandates while it can see the <see cref="JsonRpcMethodAttribute"/>
/// of the method it is dispatching to. That is only true while StreamJsonRpc is binding typed method
/// arguments itself. We deserialize request bodies manually instead (see
/// <see cref="SystemTextJsonLanguageServer{TRequestContext}"/>) so that a single method name can support
/// multiple language-specific handlers with different request types, and so that we control how
/// deserialization failures are reported. By the time we read the request body that formatter state is
/// gone, so StreamJsonRpc falls back to positional <c>[token, value]</c> arguments, which is off-spec.
/// Owning the conversion here keeps manual deserialization while still producing a compliant notification.
/// <para>
/// Note that <see cref="JsonConverter{T}.Read"/> here isn't parsing a value - it is constructing the
/// sender. We only register this on the options used to deserialize incoming requests, which creates the
/// <see cref="IProgress{T}"/> instance later used to report progress to the client, and that is what we've
/// fixed to use named arguments. The formatter's own options are left alone so that StreamJsonRpc keeps
/// handling the reverse role, where the client sends progress notifications to us: StreamJsonRpc writes an
/// <see cref="IProgress{T}"/> we send out as a token, and routes the resulting reports back to the original
/// object.
/// </para>
/// </remarks>
internal sealed class LspProgressConverterFactory : JsonConverterFactory
{
    private readonly JsonRpc _jsonRpc;

    public LspProgressConverterFactory(JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
    }

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IProgress<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(LspProgressConverter<>).MakeGenericType(valueType), _jsonRpc)!;
    }
}

internal sealed class LspProgressConverter<T> : JsonConverter<IProgress<T>>
{
    private readonly JsonRpc _jsonRpc;

    public LspProgressConverter(JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
    }

    public override IProgress<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType is not JsonTokenType.String and not JsonTokenType.Number)
        {
            throw new JsonException($"Expected a string or integer progress token, but found '{reader.TokenType}'.");
        }

        // Retain the token exactly as the client sent it.  LSP allows a progress token to be either an
        // integer or a string, and the notifications we send must echo back the same value.
        using var document = JsonDocument.ParseValue(ref reader);
        return new LspProgress(_jsonRpc, document.RootElement.Clone());
    }

    public override void Write(Utf8JsonWriter writer, IProgress<T> value, JsonSerializerOptions options)
        => throw new NotSupportedException($"{nameof(LspProgressConverter<T>)} is only used to deserialize incoming requests.");

    private sealed class LspProgress : IProgress<T>
    {
        private static readonly IReadOnlyDictionary<string, Type> s_argumentDeclaredTypes =
            new Dictionary<string, Type>
            {
                { "token", typeof(JsonElement) },
                { "value", typeof(T) },
            };

        private readonly JsonRpc _jsonRpc;
        private readonly object _boxedToken;

        public LspProgress(JsonRpc jsonRpc, JsonElement token)
        {
            _jsonRpc = jsonRpc;
            _boxedToken = token;
        }

        public void Report(T value)
        {
            // Mirrors the named-arguments branch of streamjsonrpc's MessageFormatterProgressTracker.ProgressProxy<T>,
            // which is what would run here if it were able to see that named arguments are required.
            // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#progress
            // requires $/progress to carry a named parameter object with 'token' and 'value' properties.
            var arguments = new Dictionary<string, object?>
            {
                { "token", _boxedToken },
                { "value", value },
            };

            // Unlike streamjsonrpc we declare the token as a JsonElement rather than its CLR type, since we
            // hold onto the raw token to echo back whichever form (integer or string) the client sent.
            var notifyTask = _jsonRpc.NotifyWithParameterObjectAsync(
                MessageFormatterProgressTracker.ProgressRequestSpecialMethod, arguments, s_argumentDeclaredTypes);

            // Progress notifications are fire and forget - trace failures (including the ObjectDisposedException
            // raised when the connection is torn down mid-request) instead of faulting the request that produced
            // them. Note that this is the only failure path: NotifyWithParameterObjectAsync reports errors by
            // returning a faulted task rather than by throwing synchronously.
            notifyTask.ContinueWith(
                static (task, state) => ((JsonRpc)state!).TraceSource.TraceEvent(
                    TraceEventType.Error,
                    (int)JsonRpc.TraceEvents.ProgressNotificationError,
                    "Failed to send progress update. {0}",
                    task.Exception!.InnerException ?? task.Exception),
                _jsonRpc,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default).Forget();
        }
    }
}
