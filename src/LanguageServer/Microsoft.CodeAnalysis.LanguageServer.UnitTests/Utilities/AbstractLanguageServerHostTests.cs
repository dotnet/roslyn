// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipelines;
using System.IO.Pipes;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Services;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public abstract class AbstractLanguageServerHostTests : IDisposable
{
    protected ITestOutputHelper TestOutputHelper { get; }
    protected ILoggerFactory LoggerFactory { get; }
    protected TempRoot TempRoot { get; }

    /// <summary>
    /// Snapshot of the file watches active before this test ran. Used to verify that the server releases every file
    /// watch it created once it shuts down (see <see cref="FileWatcherReleaseTracker"/>).
    /// </summary>
    private readonly FileWatcherReleaseTracker _fileWatcherReleaseTracker;

    internal static ServerConfiguration DefaultServerConfiguration => new(
        LaunchDebugger: false,
        InitialLogLevel: LogLevel.Trace,
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
        TestOutputHelper = testOutputHelper;
        LoggerFactory = new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
        TempRoot = new();
        _fileWatcherReleaseTracker = FileWatcherReleaseTracker.Capture();
    }

    private protected Task<SingleServerTestLspServer> CreateLanguageServerAsync(
        ClientCapabilities? clientCapabilities = null,
        ServerConfiguration? serverConfiguration = null)
    {
        return SingleServerTestLspServer.CreateAsync(clientCapabilities ?? new ClientCapabilities(), LoggerFactory, serverConfiguration ?? DefaultServerConfiguration, this);
    }

    /// <summary>
    /// Starts a language server daemon (the same multi-client connection manager + named-pipe listener that
    /// Program.cs runs in daemon mode) and returns a handle that can connect <see cref="TestLspServer"/> clients
    /// to it. Mirrors single-server mode (<see cref="CreateLanguageServerAsync"/>), but the returned daemon hosts
    /// many independent servers, one per connected client.
    /// </summary>
    private protected async Task<TestDaemon> CreateDaemonServerAsync(
        TimeSpan? keepAlive = null,
        ServerConfiguration? serverConfiguration = null)
    {
        var configuration = serverConfiguration ?? (ServerConfigurationWithoutDevKit with { IsDaemon = true });
        Contract.ThrowIfFalse(configuration.IsDaemon);
        var extensionManager = ExtensionAssemblyManager.Create(configuration, LoggerFactory);
        var assemblyLoader = new CustomExportAssemblyLoader(extensionManager, LoggerFactory);
        var exportProvider = await CreateExportProviderAsync(configuration, LoggerFactory, extensionManager, assemblyLoader);
        var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, LoggerFactory);
        return TestDaemon.Create(
            exportProvider,
            typeRefResolver,
            keepAlive ?? Timeout.InfiniteTimeSpan,
            LoggerFactory,
            TestOutputHelper);
    }

    internal static async Task WaitForConditionAsync(Func<bool> condition)
    {
        while (!condition())
        {
            await Task.Delay(50);
        }
    }

    public void Dispose()
    {
        TempRoot.Dispose();

        // The test's server(s) are disposed by the test body (via 'await using'), which releases their file watches on
        // shutdown. Verify that actually happened so a watch-leaking test fails here rather than leaking into a later test.
        _fileWatcherReleaseTracker.AssertWatchesReleased();
    }

    /// <summary>
    /// A connected LSP client used by tests, plus the shared request/notification plumbing over its JSON-RPC
    /// connection. Concrete modes differ in how the server they talk to is hosted: <see cref="SingleServerTestLspServer"/>
    /// owns an in-process server over in-memory pipes, while <see cref="DaemonClientTestLspServer"/> connects to a
    /// <see cref="TestDaemon"/> over a named pipe.
    /// </summary>
    protected abstract class TestLspServer : ILspClient, IAsyncDisposable
    {
        private readonly JsonRpc _clientRpc;

        public ExportProvider ExportProvider { get; }

        internal ServerCapabilities ServerCapabilities { get => field ?? throw new InvalidOperationException("Initialize has not been called"); private set; }

        internal event Action<LogMessageParams>? LogMessageReceived;

        private protected TestLspServer(ExportProvider exportProvider, Stream clientOutputStream, Stream clientInputStream, ITestOutputHelper testOutputHelper)
        {
            ExportProvider = exportProvider;

            _clientRpc = CreateClientRpc(clientOutputStream, clientInputStream, testOutputHelper);
            _clientRpc.StartListening();
        }

        private JsonRpc CreateClientRpc(Stream outputStream, Stream inputStream, ITestOutputHelper testOutputHelper)
        {
            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            var clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            if (testOutputHelper is not null)
                clientRpc.AddLocalRpcMethod(Methods.WindowLogMessageName, (int type, string message) => HandleWindowLogMessage(type, message, testOutputHelper));

            return clientRpc;
        }

        private protected async Task InitializeAsync(ClientCapabilities clientCapabilities)
        {
            var initializeResponse = await ExecuteRequestAsync<InitializeParams, InitializeResult>(
                Methods.InitializeName,
                new InitializeParams { Capabilities = clientCapabilities },
                CancellationToken.None);
            Assert.NotNull(initializeResponse?.Capabilities);
            ServerCapabilities = initializeResponse.Capabilities;

            await ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);
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
            => GetServerForLspServices().GetLspServices().GetRequiredService<T>();

        /// <summary>The language server host whose MEF services back <see cref="GetRequiredLspService{T}"/>.</summary>
        private protected abstract LanguageServerHost GetServerForLspServices();

        /// <summary>The JSON-RPC connection to the server, exposed so a subclass can abruptly drop it (crash simulation).</summary>
        private protected JsonRpc ClientRpc => _clientRpc;

        /// <summary>Whether <see cref="DisposeAsync"/> performs the clean LSP shutdown/exit handshake.</summary>
        private protected virtual bool ShouldShutDownCleanly => true;

        /// <summary>Waits for a mode's owned server to finish exiting after the shutdown handshake (no-op by default).</summary>
        private protected virtual Task WaitForServerShutdownAsync() => Task.CompletedTask;

        /// <summary>Disposes a mode's transport (e.g. a named-pipe client stream) after the RPC is torn down (no-op by default).</summary>
        private protected virtual ValueTask DisposeTransportAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            if (ShouldShutDownCleanly)
            {
                await _clientRpc.InvokeAsync(Methods.ShutdownName);
                await _clientRpc.NotifyAsync(Methods.ExitName);
                await WaitForServerShutdownAsync();
            }

            _clientRpc.Dispose();
            await DisposeTransportAsync();
        }
    }

    /// <summary>
    /// Single-server mode: owns the connection manager and the in-memory pipe pair it talks over, using the same
    /// <see cref="LanguageServerConnectionManager.RunAsync"/> entry point Program.cs uses (one in-memory connection,
    /// no keepalive). Mirrors how the standalone server runs when not in daemon mode.
    /// </summary>
    protected sealed class SingleServerTestLspServer : TestLspServer
    {
        private readonly LanguageServerConnectionManager _connectionManager;
        private readonly Task _serverTask;
        private readonly Pipe _clientToServerPipe;
        private readonly Pipe _serverToClientPipe;

        internal static async Task<SingleServerTestLspServer> CreateAsync(
            ClientCapabilities clientCapabilities,
            ILoggerFactory loggerFactory,
            ServerConfiguration serverConfiguration,
            AbstractLanguageServerHostTests hostTests)
        {
            var extensionManager = ExtensionAssemblyManager.Create(serverConfiguration, loggerFactory);
            var assemblyLoader = new CustomExportAssemblyLoader(extensionManager, loggerFactory);

            var exportProvider = await hostTests.CreateExportProviderAsync(serverConfiguration, loggerFactory, extensionManager, assemblyLoader);
            var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, loggerFactory);

            var clientToServerPipe = new Pipe();
            var serverToClientPipe = new Pipe();
            var testLspServer = new SingleServerTestLspServer(
                exportProvider, typeRefResolver, loggerFactory, hostTests.TestOutputHelper, clientToServerPipe, serverToClientPipe);

            await testLspServer.InitializeAsync(clientCapabilities);
            return testLspServer;
        }

        private SingleServerTestLspServer(
            ExportProvider exportProvider,
            ExtensionTypeRefResolver typeRefResolver,
            ILoggerFactory loggerFactory,
            ITestOutputHelper testOutputHelper,
            Pipe clientToServerPipe,
            Pipe serverToClientPipe)
            : base(exportProvider, clientToServerPipe.Writer.AsStream(), serverToClientPipe.Reader.AsStream(), testOutputHelper)
        {
            _clientToServerPipe = clientToServerPipe;
            _serverToClientPipe = serverToClientPipe;

            var serverInputStream = clientToServerPipe.Reader.AsStream();
            var serverOutputStream = serverToClientPipe.Writer.AsStream();

            _connectionManager = new LanguageServerConnectionManager();

            // Use the same RunAsync entry point that Program.cs uses, so tests exercise the real startup,
            // connection, and exit handling. Single-server mode is a source that yields this one in-memory
            // connection with no keepalive.
            var connectionSource = new SingleLanguageServerConnectionSource(new LanguageServerConnection(serverInputStream, serverOutputStream));
            var logger = loggerFactory.CreateLogger<LanguageServerConnectionManager>();
            _serverTask = _connectionManager.RunAsync(
                connectionSource, Timeout.InfiniteTimeSpan, exportProvider, typeRefResolver, logger, CancellationToken.None);
        }

        /// <summary>The host task; completes once the single in-memory server exits.</summary>
        public Task ServerExitTask => _serverTask;

        public Pipe ClientToServerPipe => _clientToServerPipe;

        public Pipe ServerToClientPipe => _serverToClientPipe;

        private protected override LanguageServerHost GetServerForLspServices()
        {
            var servers = _connectionManager.GetStartedServers();
            Contract.ThrowIfTrue(servers.IsEmpty, "No started servers found.");
            return servers.Single();
        }

        // This server owns the host task, which completes once the shutdown/exit it just sent are handled.
        private protected override Task WaitForServerShutdownAsync() => _serverTask;
    }

    /// <summary>
    /// Daemon-client mode: connects to a <see cref="TestDaemon"/> over a named pipe. The daemon owns the connection
    /// manager and spins up a dedicated server for this connection, which is associated with this client via
    /// <see cref="AttachDaemonServer"/>.
    /// </summary>
    protected sealed class DaemonClientTestLspServer : TestLspServer
    {
        private readonly NamedPipeClientStream _daemonClientStream;
        private LanguageServerHost? _daemonServer;
        private bool _crashed;

        internal static async Task<DaemonClientTestLspServer> CreateAsync(
            string pipeName,
            ExportProvider exportProvider,
            ClientCapabilities clientCapabilities,
            ITestOutputHelper testOutputHelper)
        {
            var clientStream = NamedPipeUtil.CreateClient(serverName: ".", pipeName, PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);
            try
            {
                await clientStream.ConnectAsync();
            }
            catch
            {
                await clientStream.DisposeAsync();
                throw;
            }

            var testLspServer = new DaemonClientTestLspServer(exportProvider, clientStream, testOutputHelper);
            await testLspServer.InitializeAsync(clientCapabilities);
            return testLspServer;
        }

        private DaemonClientTestLspServer(ExportProvider exportProvider, NamedPipeClientStream daemonClientStream, ITestOutputHelper testOutputHelper)
            : base(exportProvider, daemonClientStream, daemonClientStream, testOutputHelper)
        {
            _daemonClientStream = daemonClientStream;
        }

        /// <summary>Associates this client with the specific server the daemon created for its connection.</summary>
        internal void AttachDaemonServer(LanguageServerHost server) => _daemonServer = server;

        private protected override LanguageServerHost GetServerForLspServices()
        {
            Contract.ThrowIfNull(_daemonServer, "The daemon server has not been attached to this client.");
            return _daemonServer;
        }

        /// <summary>
        /// Simulates this client's process crashing: abruptly drops the transport without a clean LSP shutdown. The
        /// daemon detects the dropped connection and isolates the fault to this client's server.
        /// </summary>
        public async ValueTask CrashAsync()
        {
            _crashed = true;
            ClientRpc.Dispose();
            await _daemonClientStream.DisposeAsync();
        }

        // A "crashed" client has already dropped its transport, so skip the clean shutdown/exit handshake on dispose.
        private protected override bool ShouldShutDownCleanly => !_crashed;

        private protected override ValueTask DisposeTransportAsync() => _daemonClientStream.DisposeAsync();
    }

    /// <summary>
    /// A running language server daemon (the multi-client connection manager + named-pipe listener that Program.cs
    /// runs in daemon mode) plus a factory for connecting <see cref="TestLspServer"/> clients to it. Disposing it
    /// shuts the daemon down.
    /// </summary>
    protected sealed class TestDaemon : IAsyncDisposable
    {
        private readonly string _pipeName;
        private readonly LanguageServerConnectionManager _connectionManager;
        private readonly Task _daemonTask;
        private readonly CancellationTokenSource _cts;
        private readonly ExportProvider _exportProvider;
        private readonly ITestOutputHelper _testOutputHelper;

        internal static TestDaemon Create(
            ExportProvider exportProvider,
            ExtensionTypeRefResolver typeRefResolver,
            TimeSpan keepAlive,
            ILoggerFactory loggerFactory,
            ITestOutputHelper testOutputHelper)
        {
            var pipeName = "roslyn-daemon-test." + Guid.NewGuid().ToString("N");
            var logger = loggerFactory.CreateLogger<LanguageServerConnectionManager>();
            Contract.ThrowIfFalse(
                NamedPipeDaemonConnectionSource.TryCreate(pipeName, logger, out var source),
                "Unexpectedly failed to become the daemon for a fresh pipe name.");

            var connectionManager = new LanguageServerConnectionManager();
            var cts = new CancellationTokenSource();
            var daemonTask = Task.Run(async () =>
            {
                try
                {
                    await connectionManager.RunAsync(
                        source,
                        keepAlive,
                        exportProvider,
                        typeRefResolver,
                        logger,
                        cts.Token).ConfigureAwait(false);
                }
                finally
                {
                    source.Dispose();
                }
            });

            return new TestDaemon(pipeName, connectionManager, daemonTask, cts, exportProvider, testOutputHelper);
        }

        private TestDaemon(
            string pipeName,
            LanguageServerConnectionManager connectionManager,
            Task daemonTask,
            CancellationTokenSource cts,
            ExportProvider exportProvider,
            ITestOutputHelper testOutputHelper)
        {
            _pipeName = pipeName;
            _connectionManager = connectionManager;
            _daemonTask = daemonTask;
            _cts = cts;
            _exportProvider = exportProvider;
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>The pipe the daemon listens on. Identifies the daemon for single-instance checks.</summary>
        internal string PipeName => _pipeName;

        /// <summary>Whether a daemon currently holds the server mutex for this pipe.</summary>
        internal bool IsRunning => DaemonServerMutex.IsRunning(_pipeName);

        /// <summary>Completes when the daemon exits (its keepalive elapsed with no clients, or it was disposed).</summary>
        internal Task DaemonExitTask => _daemonTask;

        /// <summary>The language servers the daemon currently has running, one per connected client.</summary>
        internal ImmutableArray<LanguageServerHost> GetStartedServers() => _connectionManager.GetStartedServers();

        /// <summary>Exposes the connection manager's test-only API for injecting startup failures.</summary>
        internal LanguageServerConnectionManager.TestAccessor GetConnectionManagerTestAccessor()
            => _connectionManager.GetTestAccessor();

        /// <summary>
        /// Connects a new client to the daemon, initializes it, and associates it with the server the daemon spun up
        /// for the connection. Clients are created one at a time (the new server is correlated by diffing the daemon's
        /// server set, which assumes a single connection is in flight).
        /// </summary>
        internal async Task<DaemonClientTestLspServer> CreateClientAsync(ClientCapabilities? clientCapabilities = null)
        {
            var serversBefore = _connectionManager.GetStartedServers();
            var client = await DaemonClientTestLspServer.CreateAsync(_pipeName, _exportProvider, clientCapabilities ?? new ClientCapabilities(), _testOutputHelper);

            LanguageServerHost? newServer = null;
            await WaitForConditionAsync(() =>
            {
                var added = _connectionManager.GetStartedServers().Where(s => !serversBefore.Contains(s)).ToArray();
                if (added.Length != 1)
                    return false;

                newServer = added[0];
                return true;
            });

            client.AttachDaemonServer(newServer!);
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            await _daemonTask;
            _cts.Dispose();
        }
    }
}
