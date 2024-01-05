// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework.Example;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestExampleLanguageServer : ExampleLanguageServer
{
    private readonly JsonRpc _clientRpc;

    public TestExampleLanguageServer(Stream clientSteam, JsonRpc jsonRpc, ILspLogger logger, Action<IServiceCollection>? addExtraHandlers) : base(jsonRpc, logger, addExtraHandlers)
    {
        _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientSteam, clientSteam, CreateJsonMessageFormatter()))
        {
            ExceptionStrategy = ExceptionProcessing.ISerializable,
        };

        _clientRpc.Disconnected += _clientRpc_Disconnected;

        // This spins up the queue and ensure the LSP is ready to start receiving requests
        Initialize();
    }

    public async Task<TResponse> ExecuteRequestAsync<TRequest, TResponse>(string methodName, TRequest request, CancellationToken cancellationToken)
    {
        var result = await _clientRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, request, cancellationToken);

        return result;
    }

    internal async Task ExecuteNotificationAsync(string methodName, CancellationToken _)
    {
        await _clientRpc.NotifyAsync(methodName);
    }

    protected override ILifeCycleManager GetLifeCycleManager()
    {
        return new TestLifeCycleManager(_shuttingDown, _exiting);
    }

    private class TestLifeCycleManager : ILifeCycleManager
    {
        private readonly TaskCompletionSource<int> _shuttingDownSource;
        private readonly TaskCompletionSource<int> _exitingSource;

        public TestLifeCycleManager(TaskCompletionSource<int> shuttingDownSource, TaskCompletionSource<int> exitingSource)
        {
            _shuttingDownSource = shuttingDownSource;
            _exitingSource = exitingSource;
        }

        public Task ExitAsync()
        {
            _exitingSource.SetResult(0);
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(string message = "Shutting down")
        {
            _shuttingDownSource.SetResult(0);
            return Task.CompletedTask;
        }
    }

    private readonly TaskCompletionSource<int> _shuttingDown = new TaskCompletionSource<int>();
    private readonly TaskCompletionSource<int> _exiting = new TaskCompletionSource<int>();

    protected override ILspServices ConstructLspServices()
    {
        return base.ConstructLspServices();
    }

    private void _clientRpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
    {
        throw new NotImplementedException();
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

    private static JsonMessageFormatter CreateJsonMessageFormatter()
    {
        var messageFormatter = new JsonMessageFormatter();
        messageFormatter.JsonSerializer.AddVSInternalExtensionConverters();
        return messageFormatter;
    }

    internal static TestExampleLanguageServer CreateBadLanguageServer(ILspLogger logger)
    {
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();

        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream, CreateJsonMessageFormatter()));

        var extraHandlers = (IServiceCollection serviceCollection) =>
            {
                serviceCollection.AddSingleton<IMethodHandler, ExtraDidOpenHandler>();
            };

        var server = new TestExampleLanguageServer(clientStream, jsonRpc, logger, extraHandlers);

        jsonRpc.StartListening();
        server.InitializeTest();
        return server;
    }

    internal static TestExampleLanguageServer CreateLanguageServer(ILspLogger logger)
    {
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();

        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream, CreateJsonMessageFormatter()));

        var server = new TestExampleLanguageServer(clientStream, jsonRpc, logger, addExtraHandlers: null);

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

[LanguageServerEndpoint(Methods.TextDocumentDidOpenName)]
public class ExtraDidOpenHandler :
    IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>
{
    public bool MutatesSolutionState => throw new System.NotImplementedException();

    Task<SemanticTokensDeltaPartialResult> IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>.HandleRequestAsync(DidOpenTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
