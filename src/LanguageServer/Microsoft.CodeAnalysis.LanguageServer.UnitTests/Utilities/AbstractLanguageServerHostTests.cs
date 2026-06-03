// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using System.IO.Pipelines;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public abstract class AbstractLanguageServerHostTests : IDisposable
{
    protected ITestOutputHelper TestOutputHelper { get; }
    protected ILoggerFactory LoggerFactory { get; }
    protected TempRoot TempRoot { get; }
    protected TempDirectory MefCacheDirectory { get; }

    protected AbstractLanguageServerHostTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        LoggerFactory = new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
        TempRoot = new();
        MefCacheDirectory = TempRoot.CreateDirectory();
    }

    private protected Task<TestLspServer> CreateLanguageServerAsync(
        ClientCapabilities? clientCapabilities = null,
        bool includeDevKitComponents = true,
        string[]? extensionPaths = null)
    {
        return TestLspServer.CreateAsync(clientCapabilities ?? new ClientCapabilities(), LoggerFactory, MefCacheDirectory.Path, includeDevKitComponents, extensionPaths, testOutputHelper: TestOutputHelper);
    }

    public void Dispose()
    {
        TempRoot.Dispose();
    }

    protected sealed class TestLspServer : ILspClient, IAsyncDisposable
    {
        private readonly Task _languageServerHostCompletionTask;
        private readonly JsonRpc _clientRpc;
        private readonly Pipe _clientToServerPipe;
        private readonly Pipe _serverToClientPipe;

        internal static async Task<TestLspServer> CreateAsync(ClientCapabilities clientCapabilities, ILoggerFactory loggerFactory, string cacheDirectory, bool includeDevKitComponents, string[]? extensionPaths, ITestOutputHelper testOutputHelper)
        {
            var (exportProvider, assemblyLoader) = await LanguageServerTestComposition.CreateExportProviderAsync(
                loggerFactory, includeDevKitComponents, cacheDirectory, extensionPaths);
            var testLspServer = new TestLspServer(exportProvider, loggerFactory, assemblyLoader, testOutputHelper);
            var initializeResponse = await testLspServer.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = clientCapabilities }, CancellationToken.None);
            Assert.NotNull(initializeResponse?.Capabilities);
            testLspServer.ServerCapabilities = initializeResponse.Capabilities;

            await testLspServer.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);

            return testLspServer;
        }

        private readonly LanguageServerConnectionManager _connectionManager;
        public ExportProvider ExportProvider { get; }

        internal ServerCapabilities ServerCapabilities { get => field ?? throw new InvalidOperationException("Initialize has not been called"); private set; }

        private TestLspServer(ExportProvider exportProvider, ILoggerFactory loggerFactory, IAssemblyLoader assemblyLoader, ITestOutputHelper testOutputHelper)
        {
            var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, loggerFactory);

            _clientToServerPipe = new Pipe();
            _serverToClientPipe = new Pipe();

            var serverInputStream = _clientToServerPipe.Reader.AsStream();
            var serverOutputStream = _serverToClientPipe.Writer.AsStream();
            var clientOutputStream = _clientToServerPipe.Writer.AsStream();
            var clientInputStream = _serverToClientPipe.Reader.AsStream();

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientOutputStream, clientInputStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            if (testOutputHelper is not null)
                _clientRpc.AddLocalRpcMethod(Methods.WindowLogMessageName, (int type, string message) => HandleWindowLogMessage(type, message, testOutputHelper));

            _clientRpc.StartListening();

            _connectionManager = new LanguageServerConnectionManager();
            _ = _connectionManager.CreateLanguageServerHost(serverInputStream, serverOutputStream, exportProvider, typeRefResolver);

            _languageServerHostCompletionTask = _connectionManager.WaitForExitAsync();
            ExportProvider = exportProvider;
        }

        public Task ServerExitTask => _languageServerHostCompletionTask;

        public Pipe ClientToServerPipe => _clientToServerPipe;
        public Pipe ServerToClientPipe => _serverToClientPipe;

        internal event Action<LogMessageParams>? LogMessageReceived;

        public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(string methodName, TRequestType request, CancellationToken cancellationToken) where TRequestType : class
        {
            var result = await _clientRpc.InvokeWithParameterObjectAsync<TResponseType>(methodName, request, cancellationToken: cancellationToken);
            return result;
        }

        public Task ExecuteNotificationAsync<RequestType>(string methodName, RequestType request) where RequestType : class
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName, request);
        }

        public Task ExecuteNotification0Async(string methodName)
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName);
        }

        public void AddClientLocalRpcTarget(object target)
        {
            _clientRpc.AddLocalRpcTarget(target);
        }

        public void AddClientLocalRpcTarget(string methodName, Delegate handler)
        {
            _clientRpc.AddLocalRpcMethod(methodName, handler);
        }

        private void HandleWindowLogMessage(int type, string message, ITestOutputHelper testOutputHelper)
        {
            var logMessageParams = new LogMessageParams
            {
                MessageType = (MessageType)type,
                Message = message,
            };

            LogMessageReceived?.Invoke(logMessageParams);

            testOutputHelper.WriteLine($"[window/LogMessage][{(MessageType)type}] {message}");
        }

        internal T GetRequiredLspService<T>() where T : class
        {
            T? result = null;
            _connectionManager.ForEachStartedServer(server =>
            {
                result = server.GetLspServices().GetRequiredService<T>();
                return true;
            });
            return result ?? throw new InvalidOperationException("No started server found.");
        }

        public async ValueTask DisposeAsync()
        {
            await _clientRpc.InvokeAsync(Methods.ShutdownName);
            await _clientRpc.NotifyAsync(Methods.ExitName);

            // The language server host task should complete once shutdown and exit are called.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            await _languageServerHostCompletionTask;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

            _clientRpc.Dispose();
        }
    }
}
