// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

public partial class AbstractLanguageServerClientTests
{
    internal abstract partial class TestLspClient
    {
        /// <summary>
        /// Launches a thin client in daemon mode over stdio. The thin client discovers (or launches) the shared daemon
        /// named by <see cref="LspServerLaunchOptions.DaemonPipeName"/> and relays the editor's stdio to it. The
        /// returned client's "server process" is the shared daemon, which outlives any single client.
        /// </summary>
        internal static async Task<DaemonStdioLspClient> CreateDaemonStdioAsync(
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
            var lspClient = new DaemonStdioLspClient(thinClientProcess, serverProcessTaskCompletionSource.Task, workspaceContent, workspaceRootPath, workDoneProgressTarget, locations ?? [], loggerFactory, launchOptions.InitializeProcessId);

            return await InitializeAsync(lspClient, serverProcessTaskCompletionSource, clientCapabilities, workspaceRootPath);
        }

        /// <summary>
        /// Launches a thin client in daemon mode over a named pipe. The test owns the editor-side pipe; the thin client
        /// connects to it and relays to the shared daemon named by <see cref="LspServerLaunchOptions.DaemonPipeName"/>.
        /// The returned client's "server process" is the shared daemon, which outlives any single client.
        /// </summary>
        internal static async Task<DaemonPipeClient> CreateDaemonPipeAsync(
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

            // The daemon-mode thin client establishes its daemon connection before connecting to the editor pipe.
            // Race the editor-pipe connection against thin-client exit so daemon startup/connect failures fail
            // immediately instead of waiting for the pipe timeout.
            var pipeWaitTask = WaitForPipeServerConnectionAsync(pipeServer);
            var thinClientExitTask = thinClientProcess.WaitForExitAsync();
            var completedTask = await Task.WhenAny(pipeWaitTask, thinClientExitTask);
            if (thinClientExitTask == completedTask)
            {
                throw new InvalidOperationException("Thin client process exited before the pipe server connection was established.");
            }

            var serverProcessTaskCompletionSource = new TaskCompletionSource<Process>();
            var lspClient = new DaemonPipeClient(thinClientProcess, pipeServer, serverProcessTaskCompletionSource.Task, workspaceContent, workspaceRootPath, workDoneProgressTarget, locations ?? [], loggerFactory, launchOptions.InitializeProcessId);

            return await InitializeAsync(lspClient, serverProcessTaskCompletionSource, clientCapabilities, workspaceRootPath);
        }
    }

    /// <summary>
    /// Shared behavior for daemon-mode test clients. Unlike single-server mode, the "server process" is a daemon shared
    /// across clients, so disposing one client must not wait for that daemon to exit - only for its own thin client.
    /// </summary>
    internal abstract class DaemonLspClient : TestLspClient
    {
        protected DaemonLspClient(
            Process thinClientProcess,
            Stream sendingStream,
            Stream receivingStream,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory,
            int? initializeProcessId)
            : base(
                thinClientProcess,
                sendingStream,
                receivingStream,
                serverProcessTask,
                workspaceContent,
                workspaceRootPath,
                workDoneProgressTarget,
                locations,
                loggerFactory,
                initializeProcessId)
        {
        }

        protected override async Task ShutdownAndWaitForExitAsync(Process serverProcess)
        {
            // Best-effort clean shutdown of just this client's logical server; the shared daemon keeps serving others.
            if (!ThinClientProcess.HasExited && !serverProcess.HasExited)
            {
                try
                {
                    await ShutdownAndExitAsync();
                }
                catch
                {
                    // The connection may already be tearing down (e.g. the daemon or a thin client was killed); ignore.
                }
            }

            // Close our editor side so the thin client's relay observes both sides closing and exits cleanly. Then wait
            // only for the thin client to exit - never for the shared daemon, which intentionally outlives any client.
            CloseEditorTransport();
            await ThinClientProcess.WaitForExitAsync();
        }
    }

    internal sealed class DaemonStdioLspClient : DaemonLspClient
    {
        internal DaemonStdioLspClient(
            Process thinClientProcess,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory,
            int? initializeProcessId)
            : base(
                thinClientProcess,
                thinClientProcess.StandardInput.BaseStream,
                thinClientProcess.StandardOutput.BaseStream,
                serverProcessTask,
                workspaceContent,
                workspaceRootPath,
                workDoneProgressTarget,
                locations,
                loggerFactory,
                initializeProcessId)
        {
        }
    }

    internal sealed class DaemonPipeClient : DaemonLspClient
    {
        private readonly NamedPipeServerStream _pipeServer;

        internal DaemonPipeClient(
            Process thinClientProcess,
            NamedPipeServerStream pipeServer,
            Task<Process> serverProcessTask,
            LspWorkspaceContent workspaceContent,
            string workspaceRootPath,
            WorkDoneProgressTarget workDoneProgressTarget,
            Dictionary<string, IList<LSP.Location>> locations,
            ILoggerFactory loggerFactory,
            int? initializeProcessId)
            : base(
                thinClientProcess,
                pipeServer,
                pipeServer,
                serverProcessTask,
                workspaceContent,
                workspaceRootPath,
                workDoneProgressTarget,
                locations,
                loggerFactory,
                initializeProcessId)
        {
            _pipeServer = pipeServer;
        }

        protected override void DisposeTransport()
        {
            _pipeServer.Dispose();
        }
    }
}
