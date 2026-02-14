// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public partial class AbstractLanguageServerClientTests
{
    internal sealed class TestLspClient : ILspClient, IAsyncDisposable
    {
        internal const int TimeOutMsNewProcess = 60_000;

        private int _disposed = 0;

        private readonly Process _process;
        private readonly Dictionary<DocumentUri, SourceText> _documents;
        private readonly Dictionary<string, IList<LSP.Location>> _locations;
        private readonly ILoggerFactory _loggerFactory;

        private readonly JsonRpc _clientRpc;

        private ServerCapabilities? _serverCapabilities;

        internal static async Task<TestLspClient> CreateAsync(
            ClientCapabilities clientCapabilities,
            string extensionLogsPath,
            bool includeDevKitComponents,
            bool debugLsp,
            ILoggerFactory loggerFactory,
            Dictionary<DocumentUri, SourceText>? documents = null,
            Dictionary<string, IList<LSP.Location>>? locations = null)
        {
            var pipeName = CreateNewPipeName();
            var fullPipePath = GetFullPipePath(pipeName);

            // Create the pipe server - the LSP server process will connect to this as a client.
            var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            var processStartInfo = CreateLspStartInfo(fullPipePath, extensionLogsPath, includeDevKitComponents, debugLsp);

            var process = Process.Start(processStartInfo);
            Assert.NotNull(process);

            // Wait for the server process to connect to our pipe.
            using var cts = new CancellationTokenSource(TimeOutMsNewProcess);
            await pipeServer.WaitForConnectionAsync(cts.Token);

            var lspClient = new TestLspClient(process, pipeServer, documents ?? [], locations ?? [], loggerFactory);

            // We've subscribed to Disconnected, but if the process crashed before that point we might have not seen it
            if (process.HasExited)
            {
                throw new Exception($"LSP process exited immediately with {process.ExitCode}");
            }

            // Initialize the capabilities.
            var initializeResponse = await lspClient.Initialize(clientCapabilities);
            Assert.NotNull(initializeResponse?.Capabilities);
            lspClient._serverCapabilities = initializeResponse.Capabilities;

            await lspClient.Initialized();

            return lspClient;

            static string CreateNewPipeName()
            {
                // The pipe name constructed by some systems is very long (due to temp path).
                // Shorten the unique id for the pipe.
                var newGuid = Guid.NewGuid().ToString();
                return newGuid.Split('-')[0];
            }

            static string GetFullPipePath(string pipeName)
            {
                // The client creates the pipe server and passes the full pipe path to the LSP server process.
                // On Windows this is \\.\pipe\<name>, on Unix it's a socket path.
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? @"\\.\pipe\" + pipeName
                    : Path.Combine(Path.GetTempPath(), pipeName + ".sock");
            }

            static ProcessStartInfo CreateLspStartInfo(string pipeName, string extensionLogsPath, bool includeDevKitComponents, bool debugLsp)
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet",
                };

                // The LSP's runtime configuration sets rollforward to Major which allows it to run on a newer runtime
                // when the expected runtime is not present. Additionally, we need to be able to use prerelease runtimes
                // since unit tests may be running against preview builds of the .NET SDK.
                processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";

                processStartInfo.ArgumentList.Add(TestPaths.GetLanguageServerPath());

                processStartInfo.ArgumentList.Add("--pipe");
                processStartInfo.ArgumentList.Add(pipeName);

                processStartInfo.ArgumentList.Add("--logLevel");
                processStartInfo.ArgumentList.Add("Trace");

                processStartInfo.ArgumentList.Add("--extensionLogDirectory");
                processStartInfo.ArgumentList.Add(extensionLogsPath);

                if (includeDevKitComponents)
                {
                    processStartInfo.ArgumentList.Add("--devKitDependencyPath");
                    processStartInfo.ArgumentList.Add(TestPaths.GetDevKitExtensionPath());
                }

                if (debugLsp)
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

        internal ServerCapabilities ServerCapabilities => _serverCapabilities ?? throw new InvalidOperationException("Initialize has not been called");

        private TestLspClient(Process process, Stream pipeStream, Dictionary<DocumentUri, SourceText> documents, Dictionary<string, IList<LSP.Location>> locations, ILoggerFactory loggerFactory)
        {
            _documents = documents;
            _locations = locations;
            _loggerFactory = loggerFactory;
            _process = process;

            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.ErrorDataReceived += Process_ErrorDataReceived;

            // Call this last so our type is fully constructed before we start firing events
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Assert.False(_process.HasExited);

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(pipeStream, pipeStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            _clientRpc.AddLocalRpcMethod(Methods.WindowLogMessageName, GetMessageLogger("LogMessage"));
            _clientRpc.AddLocalRpcMethod(Methods.WindowShowMessageName, GetMessageLogger("ShowMessage"));

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

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _loggerFactory.CreateLogger("LSP STDOUT").LogInformation(e.Data);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _loggerFactory.CreateLogger("LSP STDERR").LogInformation(e.Data);
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

        public void ApplyWorkspaceEdit(WorkspaceEdit? workspaceEdit)
        {
            Assert.NotNull(workspaceEdit);

            // We do not support applying the following edits
            Assert.Null(workspaceEdit.Changes);
            Assert.Null(workspaceEdit.ChangeAnnotations);

            // Currently we only support applying TextDocumentEdits
            var textDocumentEdits = (TextDocumentEdit[]?)workspaceEdit.DocumentChanges?.Value;
            Assert.NotNull(textDocumentEdits);

            foreach (var documentEdit in textDocumentEdits)
            {
                var uri = documentEdit.TextDocument.DocumentUri;
                var document = _documents[uri];

                var changes = documentEdit.Edits
                    .Select(edit => edit.Value)
                    .Cast<TextEdit>()
                    .SelectAsArray(edit => ProtocolConversions.TextEditToTextChange(edit, document));

                var updatedDocument = document.WithChanges(changes);
                _documents[uri] = updatedDocument;
            }
        }

        public string GetDocumentText(DocumentUri uri) => _documents[uri].ToString();

        public IList<LSP.Location> GetLocations(string locationName) => _locations[locationName];

        public async ValueTask DisposeAsync()
        {
            // Ensure only one thing disposes; while we disconnect the process will go away, which will call us to do this again
            if (Interlocked.CompareExchange(ref _disposed, value: 1, comparand: 0) != 0)
                return;

            var logger = _loggerFactory.CreateLogger("Shutdown");

            if (!_process.HasExited)
            {
                logger.LogTrace("Sending a Shutdown request to the LSP.");

                await _clientRpc.InvokeAsync(Methods.ShutdownName);
                await _clientRpc.NotifyAsync(Methods.ExitName);

                await _clientRpc.Completion;
            }

            _clientRpc.Dispose();
            _process.Dispose();

            logger.LogTrace("Process shut down.");
        }
    }
}
