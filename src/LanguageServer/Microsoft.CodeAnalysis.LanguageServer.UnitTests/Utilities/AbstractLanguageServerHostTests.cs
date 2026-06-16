// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using System.IO.Pipelines;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.LanguageServer.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public abstract class AbstractLanguageServerHostTests : IDisposable
{
    protected ILoggerFactory LoggerFactory { get; }
    protected TempRoot TempRoot { get; }

    /// <summary>
    /// Snapshot of the file watches active before this test ran. Used to verify that the server releases every file
    /// watch it created once it shuts down (see <see cref="FileWatcherReleaseTracker"/>).
    /// </summary>
    private readonly FileWatcherReleaseTracker _fileWatcherReleaseTracker;

    internal static ServerConfiguration DefaultServerConfiguration => new(
        LaunchDebugger: false,
        LogConfiguration: new LogConfiguration(LogLevel.Trace),
        TelemetryLevel: null,
        SessionId: null,
        ExtensionAssemblyPaths: Array.Empty<string>(),
        DevKitDependencyPath: TestPaths.GetDevKitExtensionPath(),
        CSharpDesignTimePath: null,
        ExtensionLogDirectory: string.Empty,
        ServerPipeName: null,
        UseStdIo: false,
        AutoLoadProjects: null,
        SourceGeneratorExecutionPreference: SourceGeneratorExecutionPreference.Balanced,
        ClientProcessId: null);

    internal static ServerConfiguration ServerConfigurationWithoutDevKit => DefaultServerConfiguration with { DevKitDependencyPath = null };

    private protected virtual Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
    {
        var provider = LanguageServerTestComposition.GetSharedExportProvider(serverConfiguration, loggerFactory);
        return Task.FromResult(provider);
    }

    protected AbstractLanguageServerHostTests(ITestOutputHelper testOutputHelper)
    {
        LoggerFactory = new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
        TempRoot = new();
        _fileWatcherReleaseTracker = FileWatcherReleaseTracker.Capture();
    }

    private protected Task<TestLspServer> CreateLanguageServerAsync(
        ClientCapabilities? clientCapabilities = null,
        ServerConfiguration? serverConfiguration = null)
    {
        return TestLspServer.CreateAsync(clientCapabilities ?? new ClientCapabilities(), LoggerFactory, serverConfiguration ?? DefaultServerConfiguration, this);
    }

    public void Dispose()
    {
        TempRoot.Dispose();

        // The test's server(s) are disposed by the test body (via 'await using'), which releases their file watches on
        // shutdown. Verify that actually happened so a watch-leaking test fails here rather than leaking into a later test.
        _fileWatcherReleaseTracker.AssertWatchesReleased();
    }

    protected sealed class TestLspServer : ILspClient, IAsyncDisposable
    {
        private readonly Task _languageServerHostCompletionTask;
        private readonly JsonRpc _clientRpc;
        private readonly Pipe _clientToServerPipe;
        private readonly Pipe _serverToClientPipe;

        internal static async Task<TestLspServer> CreateAsync(
            ClientCapabilities clientCapabilities,
            ILoggerFactory loggerFactory,
            ServerConfiguration serverConfiguration,
            AbstractLanguageServerHostTests hostTests)
        {
            var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
            var assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);

            var exportProvider = await hostTests.CreateExportProviderAsync(serverConfiguration, loggerFactory, extensionManager, assemblyLoader);
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

            _clientToServerPipe = new Pipe();
            _serverToClientPipe = new Pipe();

            var serverInputStream = _clientToServerPipe.Reader.AsStream();
            var serverOutputStream = _serverToClientPipe.Writer.AsStream();
            var clientOutputStream = _clientToServerPipe.Writer.AsStream();
            var clientInputStream = _serverToClientPipe.Reader.AsStream();

            LanguageServerHost = new LanguageServerHost(serverInputStream, serverOutputStream, exportProvider, loggerFactory, typeRefResolver);

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientOutputStream, clientInputStream, messageFormatter))
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

        public Pipe ClientToServerPipe => _clientToServerPipe;
        public Pipe ServerToClientPipe => _serverToClientPipe;

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

        internal T GetRequiredLspService<T>() where T : class => LanguageServerHost.GetLspServices().GetRequiredService<T>();

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
