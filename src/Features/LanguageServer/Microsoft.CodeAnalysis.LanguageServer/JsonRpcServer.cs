// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer;
internal sealed class JsonRpcServer : IDisposable
{
    private readonly JsonRpc _jsonRpc;
    private readonly ILogger _logger;

    public JsonRpcServer(Stream inputStream, Stream outputStream, ILogger logger)
    {
        _logger = logger;

        var handler = new HeaderDelimitedMessageHandler(outputStream, inputStream, new JsonMessageFormatter());
        _jsonRpc = new JsonRpc(handler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        _jsonRpc.AddLocalRpcTarget(this);
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Running server...");
        _jsonRpc.StartListening();
        return _jsonRpc.Completion;
    }

    [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
    public Task<string> InitializeAsync(JToken initializeParams, CancellationToken _)
    {
        _logger.LogInformation($"Initialize request with client capabilities: {initializeParams}");
        return Task.FromResult("hello there");
    }

    public void Dispose()
    {
        _jsonRpc.Dispose();
    }
}
