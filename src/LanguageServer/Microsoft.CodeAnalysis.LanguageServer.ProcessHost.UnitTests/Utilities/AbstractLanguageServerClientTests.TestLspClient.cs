// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

public partial class AbstractLanguageServerClientTests
{
    internal abstract partial class TestLspClient : IAsyncDisposable
    {
        internal const int TimeOutMsNewProcess = 60_000;

        private int _disposed = 0;

        private readonly Process _thinClientProcess;
        private readonly Task<Process> _serverProcessTask;
        private readonly string _workspaceRootPath;
        private readonly Dictionary<string, IList<LSP.Location>> _locations;
        private readonly ILoggerFactory _loggerFactory;
        private readonly int? _initializeProcessId;
        private LspWorkspaceContent _workspaceContent;

        private readonly JsonRpc _clientRpc;

        private ServerCapabilities? _serverCapabilities;

        internal static async Task<SingleServerStdioLspClient> CreateSingleServerStdioAsync(
            ClientCapabilities clientCapabilities,
            string extensionLogsPath,
            LspServerLaunchOptions launchOptions,
            ILoggerFactory loggerFactory,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>>? locations = null)
        {
            var clientProcessId = launchOptions.ClientProcessId ?? Environment.ProcessId;
            var thinClientProcess = CreateThinClient(pipePath: null, clientProcessId, extensionLogsPath, launchOptions, loggerFactory, logOutput: false);
            var serverProcessTaskCompletionSource = new TaskCompletionSource<Process>();
            var lspClient = new SingleServerStdioLspClient(thinClientProcess, serverProcessTaskCompletionSource.Task, workspaceContent, workspaceRootPath, workDoneProgressTarget, locations ?? [], loggerFactory);

            return await InitializeAsync(lspClient, serverProcessTaskCompletionSource, clientCapabilities, workspaceRootPath);
        }

        internal static async Task<SingleServerPipeClient> CreateSingleServerPipeAsync(
            ClientCapabilities clientCapabilities,
            string extensionLogsPath,
            LspServerLaunchOptions launchOptions,
            ILoggerFactory loggerFactory,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>>? locations = null)
        {
            var (lspClientPipeName, lspServerPipeName) = GetPipePaths();
            var clientProcessId = launchOptions.ClientProcessId ?? Environment.ProcessId;

            var pipeServer = new NamedPipeServerStream(lspClientPipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            var thinClientProcess = CreateThinClient(lspServerPipeName, clientProcessId, extensionLogsPath, launchOptions, loggerFactory, logOutput: true);

            // Wait for the pipe server to connect.  Also capture if the thin client process exits before the connection is established.
            var pipeWaitTask = WaitForPipeServerConnectionAsync(pipeServer);
            var thinClientExitTask = thinClientProcess.WaitForExitAsync();
            var completedTask = await Task.WhenAny(pipeWaitTask, thinClientExitTask);
            if (thinClientExitTask == completedTask)
            {
                throw new InvalidOperationException("Thin client process exited before the pipe server connection was established.");
            }

            var serverProcessTaskCompletionSource = new TaskCompletionSource<Process>();
            var lspClient = new SingleServerPipeClient(thinClientProcess, pipeServer, serverProcessTaskCompletionSource.Task, workspaceContent, workspaceRootPath, workDoneProgressTarget, locations ?? [], loggerFactory);

            return await InitializeAsync(lspClient, serverProcessTaskCompletionSource, clientCapabilities, workspaceRootPath);
        }

        private static async Task<TClient> InitializeAsync<TClient>(
            TClient lspClient,
            TaskCompletionSource<Process> serverProcessTaskCompletionSource,
            ClientCapabilities clientCapabilities,
            string workspaceRootPath) where TClient : TestLspClient
        {
            // Initialize the capabilities.
            var initializeResponse = await lspClient.Initialize(clientCapabilities, workspaceRootPath);
            Assert.NotNull(initializeResponse?.Capabilities);

            var serverProcessId = initializeResponse.ProcessId;
            var serverProcess = Process.GetProcessById(serverProcessId);
            serverProcessTaskCompletionSource.SetResult(serverProcess);

            lspClient._serverCapabilities = initializeResponse.Capabilities;

            await lspClient.Initialized();

            return lspClient;
        }

        private static Process CreateThinClient(
            string? pipePath,
            int clientProcessId,
            string extensionLogsPath,
            LspServerLaunchOptions launchOptions,
            ILoggerFactory loggerFactory,
            bool logOutput)
        {
            var thinClientProcess = new Process();
            var processStartInfo = CreateThinClientStartInfo(pipePath, clientProcessId, extensionLogsPath, launchOptions);
            thinClientProcess.StartInfo = processStartInfo;
            thinClientProcess.ErrorDataReceived += (sender, e) => LogProcessData(sender, e, loggerFactory.CreateLogger("[ThinClient][stderr]"));

            if (logOutput)
            {
                thinClientProcess.OutputDataReceived += (sender, e) => LogProcessData(sender, e, loggerFactory.CreateLogger("[ThinClient][stdout]"));
            }

            thinClientProcess.Start();
            thinClientProcess.BeginErrorReadLine();

            if (logOutput)
            {
                thinClientProcess.BeginOutputReadLine();
            }

            return thinClientProcess;
        }

        private static (string LspClientPipeName, string LspServerPipeName) GetPipePaths()
        {
            var pipeName = NamedPipeTestUtilities.CreateShortPipeName();

            var lspServerPipeName = GetFullPipePath(pipeName);
            var lspClientPipeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? pipeName : lspServerPipeName;

            return (lspClientPipeName, lspServerPipeName);

            static string GetFullPipePath(string pipeName)
            {
                // The client creates the pipe server and passes the full pipe path to the LSP server process.
                // On Windows this is \\.\pipe\<name>, on Unix it's a socket path.
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? @"\\.\pipe\" + pipeName
                    : Path.Combine(Path.GetTempPath(), pipeName + ".sock");
            }
        }

        private static async Task<NamedPipeServerStream> WaitForPipeServerConnectionAsync(NamedPipeServerStream pipeServer)
        {
            using var cts = new CancellationTokenSource(TimeOutMsNewProcess);
            var waitForConnectionTask = pipeServer.WaitForConnectionAsync(cts.Token);

            try
            {
                await waitForConnectionTask;
            }
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out after {TimeOutMsNewProcess}ms waiting for the LSP process to connect to the pipe.", ex);
            }

            return pipeServer;
        }

        private static void LogProcessData(object _, DataReceivedEventArgs e, ILogger logger)
        {
            if (e.Data != null)
            {
                logger.LogError(e.Data);
            }
        }

        internal ServerCapabilities ServerCapabilities => _serverCapabilities ?? throw new InvalidOperationException("Initialize has not been called");
        internal LspWorkspaceContent WorkspaceContent => _workspaceContent;
        internal string WorkspaceRootPath => _workspaceRootPath;
        internal bool ProjectInitializationCompleted { get; set; }
        internal WorkDoneProgressTarget WorkDoneProgress { get; }

        private readonly TaskCompletionSource _projectInitializationCompletedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected TestLspClient(
            Process thinClientProcess,
            Stream sendingStream,
            Stream receivingStream,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory,
            int? initializeProcessId = null)
        {
            _workspaceContent = workspaceContent;
            _workspaceRootPath = Path.GetFullPath(workspaceRootPath);
            WorkDoneProgress = workDoneProgressTarget;
            _locations = locations;
            _loggerFactory = loggerFactory;
            _initializeProcessId = initializeProcessId;
            _thinClientProcess = thinClientProcess;
            _serverProcessTask = serverProcessTask;
            _thinClientProcess.EnableRaisingEvents = true;

            Assert.False(_thinClientProcess.HasExited);

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(sendingStream, receivingStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            _clientRpc.AddLocalRpcMethod(Methods.WindowLogMessageName, GetMessageLogger("LogMessage"));
            _clientRpc.AddLocalRpcMethod(Methods.WindowShowMessageName, GetMessageLogger("ShowMessage"));
            _clientRpc.AddLocalRpcTarget(workDoneProgressTarget);
            _clientRpc.AddLocalRpcMethod(ProjectInitializationHandler.ProjectInitializationCompleteName, this.OnProjectInitializationComplete);

            _clientRpc.StartListening();

            return;

            Action<int, string> GetMessageLogger(string method)
            {
                var logger = _loggerFactory.CreateLogger($"LSP {method}");

                return (type, message) =>
                {
                    var logLevel = (MessageType)type switch
                    {
                        MessageType.Error => LogLevel.Error,
                        MessageType.Warning => LogLevel.Warning,
                        MessageType.Info => LogLevel.Information,
                        MessageType.Log => LogLevel.Trace,
                        MessageType.Debug => LogLevel.Debug,
                        _ => LogLevel.Trace,
                    };

                    logger.Log(logLevel, message);
                };
            }
        }

        internal Task<Process> GetServerProcessAsync()
            => _serverProcessTask;

        internal Process ThinClientProcess => _thinClientProcess;

        internal void CloseEditorTransport()
        {
            _clientRpc.Dispose();
        }

        /// <summary>Performs a clean LSP <c>shutdown</c>/<c>exit</c> so the session ends gracefully.</summary>
        internal async Task ShutdownAndExitAsync()
        {
            await _clientRpc.InvokeAsync(Methods.ShutdownName);
            await _clientRpc.NotifyAsync(Methods.ExitName);
        }

        public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(string methodName, TRequestType request, CancellationToken cancellationToken) where TRequestType : class
        {
            var result = await _clientRpc.InvokeWithParameterObjectAsync<TResponseType>(methodName, request, cancellationToken);
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

        public Task<RoslynInitializeResult?> Initialize(ClientCapabilities clientCapabilities, string workspaceRootPath)
            => ExecuteRequestAsync<InitializeParams, RoslynInitializeResult>(Methods.InitializeName, new InitializeParams
            {
                ProcessId = _initializeProcessId,
                Capabilities = clientCapabilities,
                WorkspaceFolders =
                [
                    new WorkspaceFolder
                    {
                        DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspaceRootPath),
                        Name = Path.GetFileName(workspaceRootPath)
                    }
                ]
            }, CancellationToken.None);

        public Task Initialized()
            => ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);

        public Task OpenProjectsAsync(DocumentUri[] projects)
            => ExecuteNotificationAsync<OpenProjectHandler.NotificationParams>(OpenProjectHandler.OpenProjectName, new() { Projects = projects });

        public Task OpenSolutionAsync(DocumentUri solution)
            => ExecuteNotificationAsync<OpenSolutionHandler.NotificationParams>(OpenSolutionHandler.OpenSolutionName, new() { Solution = solution });

        public void ApplyWorkspaceEdit(WorkspaceEdit? workspaceEdit)
        {
            Assert.NotNull(workspaceEdit);

            // We do not support applying the following edits
            Assert.Null(workspaceEdit.Changes);
            Assert.Null(workspaceEdit.ChangeAnnotations);

            var documentChanges = workspaceEdit.DocumentChanges;
            Assert.NotNull(documentChanges);

            if (documentChanges.Value.TryGetFirst(out var textDocumentEdits))
            {
                foreach (var textDocumentEdit in textDocumentEdits)
                    ApplyTextDocumentEdit(textDocumentEdit);

                return;
            }

            var resourceDocumentChanges = documentChanges.Value.Second;
            foreach (var documentChange in resourceDocumentChanges)
            {
                switch (documentChange.Value)
                {
                    case CreateFile createFile:
                        _workspaceContent = _workspaceContent.WithFile(GetRelativePath(createFile.DocumentUri), string.Empty);
                        break;
                    case TextDocumentEdit textDocumentEdit:
                        ApplyTextDocumentEdit(textDocumentEdit);
                        break;
                    default:
                        Assert.Fail($"Unsupported workspace edit change: {documentChange.Value?.GetType().Name}");
                        break;
                }
            }

            return;

            void ApplyTextDocumentEdit(TextDocumentEdit documentEdit)
            {
                var uri = documentEdit.TextDocument.DocumentUri;
                var relativePath = GetRelativePath(uri);
                var document = SourceText.From(_workspaceContent.GetFileText(relativePath));

                var changes = documentEdit.Edits
                    .Select(edit => edit.Value)
                    .Cast<TextEdit>()
                    .SelectAsArray(edit => ProtocolConversions.TextEditToTextChange(edit, document));

                var updatedDocument = document.WithChanges(changes);
                _workspaceContent = _workspaceContent.WithFile(relativePath, updatedDocument.ToString());
            }
        }

        public string GetFileText(string path) => _workspaceContent.GetFileText(path);

        public string GetFileText(DocumentUri documentUri) => _workspaceContent.GetFileText(GetRelativePath(documentUri));

        public IList<LSP.Location> GetLocations(string locationName) => _locations[locationName];

        public async Task WaitForProjectInitializationAsync()
        {
            await _projectInitializationCompletedSource.Task;
            ProjectInitializationCompleted = true;
        }

        private void OnProjectInitializationComplete()
            => _projectInitializationCompletedSource.TrySetResult();

        private string GetRelativePath(DocumentUri documentUri)
        {
            var localPath = Path.GetFullPath(documentUri.GetRequiredParsedUri().LocalPath);
            var relativePath = PathUtilities.GetRelativePath(_workspaceRootPath, localPath);

            Assert.False(PathUtilities.IsAbsolute(relativePath), $"Document URI is not under the workspace root: {documentUri}");
            Assert.DoesNotContain("..", relativePath.Split(PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar));

            return LspWorkspaceContent.NormalizePath(relativePath);
        }

        public async ValueTask DisposeAsync()
        {
            // Ensure only one thing disposes; while we disconnect the process will go away, which will call us to do this again
            if (Interlocked.CompareExchange(ref _disposed, value: 1, comparand: 0) != 0)
                return;

            var logger = _loggerFactory.CreateLogger("Shutdown");

            var serverProcess = await GetServerProcessAsync();

            await ShutdownAndWaitForExitAsync(serverProcess);

            _clientRpc.Dispose();
            _thinClientProcess.Dispose();
            serverProcess.Dispose();
            DisposeTransport();

            logger.LogTrace("Process shut down.");
        }

        /// <summary>
        /// Performs the clean LSP <c>shutdown</c>/<c>exit</c> handshake (when the session is still live) and waits for
        /// the processes this client owns to exit. Single-server mode owns its dedicated server and waits for it (and
        /// the thin client); daemon mode shares a long-lived daemon and so overrides this to wait only for its own
        /// thin client.
        /// </summary>
        protected virtual async Task ShutdownAndWaitForExitAsync(Process serverProcess)
        {
            if (!_thinClientProcess.HasExited && !serverProcess.HasExited)
            {
                _loggerFactory.CreateLogger("Shutdown").LogTrace("Sending a Shutdown request to the LSP.");

                await _clientRpc.InvokeAsync(Methods.ShutdownName);
                await _clientRpc.NotifyAsync(Methods.ExitName);

                await _clientRpc.Completion;
            }

            await serverProcess.WaitForExitAsync();
            await _thinClientProcess.WaitForExitAsync();
        }

        protected virtual void DisposeTransport()
        {
        }
    }

    internal sealed class SingleServerStdioLspClient : TestLspClient
    {
        internal SingleServerStdioLspClient(
            Process thinClientProcess,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory)
            : base(
                thinClientProcess,
                thinClientProcess.StandardInput.BaseStream,
                thinClientProcess.StandardOutput.BaseStream,
                serverProcessTask,
                workspaceContent,
                workspaceRootPath,
                workDoneProgressTarget,
                locations,
                loggerFactory)
        {
        }
    }

    internal sealed class SingleServerPipeClient : TestLspClient
    {
        private readonly NamedPipeServerStream _pipeServer;

        internal SingleServerPipeClient(
            Process thinClientProcess,
            NamedPipeServerStream pipeServer,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory)
            : base(
                thinClientProcess,
                pipeServer,
                pipeServer,
                serverProcessTask,
                workspaceContent,
                workspaceRootPath,
                workDoneProgressTarget,
                locations,
                loggerFactory)
        {
            _pipeServer = pipeServer;
        }

        protected override void DisposeTransport()
        {
            _pipeServer.Dispose();
        }
    }

    private protected static ProcessStartInfo CreateThinClientStartInfo(
        string? pipePath,
        int? clientProcessId,
        string extensionLogsPath,
        LspServerLaunchOptions launchOptions)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet",
        };

        // The LSP's runtime configuration sets rollforward to Major which allows it to run on a newer runtime
        // when the expected runtime is not present. Additionally, we need to be able to use prerelease runtimes
        // since unit tests may be running against preview builds of the .NET SDK.
        processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";

        processStartInfo.ArgumentList.Add(TestPaths.GetThinClientPath());

        if (pipePath is not null)
        {
            processStartInfo.ArgumentList.Add("--pipe");
            processStartInfo.ArgumentList.Add(pipePath);
        }
        else
        {
            processStartInfo.ArgumentList.Add("--stdio");
        }

        if (clientProcessId is int processId)
        {
            processStartInfo.ArgumentList.Add("--clientProcessId");
            processStartInfo.ArgumentList.Add(processId.ToString(CultureInfo.InvariantCulture));
        }

        if (launchOptions.DaemonMode)
        {
            processStartInfo.ArgumentList.Add("--daemon-mode");

            // Scope the daemon to this test by overriding its pipe name, so independent tests never share a daemon.
            if (launchOptions.DaemonPipeName is { } daemonPipeName)
                processStartInfo.Environment[DaemonPipeName.PipeNameOverrideEnvironmentVariable] = daemonPipeName;

            // The thin client launches the daemon as a child, which inherits this environment, so the daemon picks up
            // the requested keepalive window. A non-positive (e.g. infinite) value maps to the "stay alive" sentinel.
            if (launchOptions.DaemonKeepAlive is { } daemonKeepAlive)
            {
                var keepAliveSeconds = daemonKeepAlive <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(daemonKeepAlive.TotalSeconds);
                processStartInfo.Environment[LanguageServerCommandLine.DaemonKeepAliveEnvironmentVariable] = keepAliveSeconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        processStartInfo.ArgumentList.Add("--logLevel");
        processStartInfo.ArgumentList.Add("Trace");

        processStartInfo.ArgumentList.Add("--extensionLogDirectory");
        processStartInfo.ArgumentList.Add(extensionLogsPath);

        if (launchOptions.AutoLoadProjects)
        {
            processStartInfo.ArgumentList.Add("--autoLoadProjects");
        }

        if (launchOptions.IncludeDevKitComponents)
        {
            processStartInfo.ArgumentList.Add("--devKitDependencyPath");
            processStartInfo.ArgumentList.Add(TestPaths.GetDevKitExtensionPath());
        }

        if (launchOptions.DebugLsp)
        {
            processStartInfo.ArgumentList.Add("--debug");
        }

        processStartInfo.CreateNoWindow = false;
        processStartInfo.UseShellExecute = false;
        processStartInfo.RedirectStandardInput = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        return processStartInfo;
    }
}
