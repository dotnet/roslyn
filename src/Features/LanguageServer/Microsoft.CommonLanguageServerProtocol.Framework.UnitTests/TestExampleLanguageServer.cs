// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework.Example;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestExampleLanguageServer : ExampleLanguageServer
{
    private readonly JsonRpc _clientRpc;

    public TestExampleLanguageServer(Stream clientSteam, JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger)
    {
        _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientSteam, clientSteam, CreateJsonMessageFormatter()))
        {
            ExceptionStrategy = ExceptionProcessing.ISerializable,
        };

        _clientRpc.Disconnected += _clientRpc_Disconnected;
    }

    public async Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken)
    {
        var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken);

        return result;
    }

    internal async Task ExecuteNotificationAsync(string methodName, CancellationToken _)
    {
        await _clientRpc.NotifyAsync(methodName);
    }

    private readonly TaskCompletionSource<int> _shuttingDown = new TaskCompletionSource<int>();
    private readonly TaskCompletionSource<int> _exiting = new TaskCompletionSource<int>();

    private void _clientRpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
    {
        throw new NotImplementedException();
    }

    public override async Task ShutdownAsync(string message = "Shutting down")
    {
        await base.ShutdownAsync(message);
        _shuttingDown.SetResult(0);
    }

    public override async Task ExitAsync()
    {
        await base.ExitAsync();
        _exiting.SetResult(0);
    }

    public void InitializeTest()
    {
        _clientRpc.StartListening();
    }

    public async Task<int> WaitForShutdown()
    {
        return await _shuttingDown.Task;
    }

    internal async Task<int> WaitForExit()
    {
        return await _exiting.Task;
    }

    public new ValueTask DisposeAsync()
    {
        _clientRpc.Dispose();
        return base.DisposeAsync();
    }

    private static JsonMessageFormatter CreateJsonMessageFormatter()
    {
        var messageFormatter = new JsonMessageFormatter();
        messageFormatter.JsonSerializer.AddVSInternalExtensionConverters();
        return messageFormatter;
    }

    internal static TestExampleLanguageServer CreateLanguageServer(ILspLogger logger)
    {
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();

        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream, CreateJsonMessageFormatter()));

        var server = new TestExampleLanguageServer(clientStream, jsonRpc, logger);
        server.InitializeAsync();

        jsonRpc.StartListening();
        server.InitializeTest();
        return server;
    }

    internal async Task ShutdownServerAsync()
    {
        await ExecuteNotificationAsync(Methods.ShutdownName, CancellationToken.None);
    }

    internal async Task<InitializeResult> InitializeServerAsync()
    {
        var request = new InitializeParams
        {
            Capabilities = new ClientCapabilities
            {

            },
        };

        var result = await ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, request, CancellationToken.None);

        return result;
    }
}
