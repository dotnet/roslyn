// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public abstract class AbstractLanguageServerHostTests : IDisposable
{
    protected ILoggerFactory LoggerFactory { get; }
    protected TempRoot TempRoot { get; }
    protected TempDirectory MefCacheDirectory { get; }

    protected AbstractLanguageServerHostTests(ITestOutputHelper testOutputHelper)
    {
        LoggerFactory = new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
        TempRoot = new();
        MefCacheDirectory = TempRoot.CreateDirectory();
    }

    protected Task<TestLspServer> CreateLanguageServerAsync(bool includeDevKitComponents = true)
    {
        return TestLspServer.CreateAsync(new ClientCapabilities(), LoggerFactory, MefCacheDirectory.Path, includeDevKitComponents);
    }

    public void Dispose()
    {
        TempRoot.Dispose();
    }

    protected sealed class TestLspServer : ILspClient, IAsyncDisposable
    {
        private readonly Task _languageServerHostCompletionTask;
        private readonly JsonRpc _clientRpc;
        private readonly Stream _serverStream;
        private readonly Stream _clientStream;

        internal static async Task<TestLspServer> CreateAsync(ClientCapabilities clientCapabilities, ILoggerFactory loggerFactory, string cacheDirectory, bool includeDevKitComponents = true, string[]? extensionPaths = null)
        {
            var (exportProvider, assemblyLoader) = await LanguageServerTestComposition.CreateExportProviderAsync(
                loggerFactory, includeDevKitComponents, cacheDirectory, extensionPaths);
            var testLspServer = new TestLspServer(exportProvider, loggerFactory, assemblyLoader);
            var initializeResponse = await testLspServer.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = clientCapabilities }, CancellationToken.None);
            Assert.NotNull(initializeResponse?.Capabilities);
            testLspServer.ServerCapabilities = initializeResponse.Capabilities;

            await testLspServer.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);

            return testLspServer;
        }

        internal LanguageServerHost LanguageServerHost { get; }
        public ExportProvider ExportProvider { get; }

        internal ServerCapabilities ServerCapabilities { get => field ?? throw new InvalidOperationException("Initialize has not been called"); private set; }

        private TestLspServer(ExportProvider exportProvider, ILoggerFactory loggerFactory, IAssemblyLoader assemblyLoader)
        {
            var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, loggerFactory);

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            _serverStream = serverStream;
            _clientStream = clientStream;
            LanguageServerHost = new LanguageServerHost(serverStream, serverStream, exportProvider, loggerFactory, typeRefResolver);

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            _clientRpc.StartListening();

            // This task completes when the server shuts down.  We store it so that we can wait for completion
            // when we dispose of the test server.
            LanguageServerHost.Start();

            _languageServerHostCompletionTask = LanguageServerHost.WaitForExitAsync();
            ExportProvider = exportProvider;
        }

        public Task ServerExitTask => _languageServerHostCompletionTask;

        /// <summary>
        /// Simulates the transport layer failing by closing the server's stream.
        /// This forces an exception in the JsonRpc read loop.
        /// </summary>
        public void SimulateStreamReadError()
        {
            _serverStream.Close();
        }

        /// <summary>
        /// Simulates the client disconnecting abruptly by closing the client stream.
        /// </summary>
        public void SimulateClientDisconnectError()
        {
            _clientStream.Close();
        }

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
