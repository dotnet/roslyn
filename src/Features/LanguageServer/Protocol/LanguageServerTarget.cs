// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.RequestExecutionQueue;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class LanguageServerTarget : ILanguageServerTarget
    {
        private readonly ICapabilitiesProvider _capabilitiesProvider;

        private readonly JsonRpc _jsonRpc;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly RequestExecutionQueue _queue;
        private readonly LanguageServerNotificationManager _notificationManager;
        private readonly IAsynchronousOperationListener _listener;
        private readonly RequestTelemetryLogger _requestTelemetryLogger;
        private readonly ILspLogger _logger;

        // Set on first LSP initialize request.
        private ClientCapabilities? _clientCapabilities;

        // Fields used during shutdown.
        private bool _shuttingDown;
        private Task? _errorShutdownTask;

        internal bool HasShutdownStarted => _shuttingDown;

        internal LanguageServerTarget(
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            LspWorkspaceRegistrationService workspaceRegistrationService,
            LspMiscellaneousFilesWorkspace? lspMiscellaneousFilesWorkspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
        {
            _requestDispatcher = requestDispatcherFactory.CreateRequestDispatcher(serverKind);

            _capabilitiesProvider = capabilitiesProvider;
            _logger = logger;

            _jsonRpc = jsonRpc;
            _jsonRpc.AddLocalRpcTarget(this);
            _jsonRpc.Disconnected += JsonRpc_Disconnected;

            _notificationManager = new LanguageServerNotificationManager(_jsonRpc);
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);

            // Pass the language client instance type name to the telemetry logger to ensure we can
            // differentiate between the different C# LSP servers that have the same client name.
            // We also don't use the language client's name property as it is a localized user facing string
            // which is difficult to write telemetry queries for.
            _requestTelemetryLogger = new RequestTelemetryLogger(serverKind.ToTelemetryString());
            _lspWorkspaceManager = new LspWorkspaceManager(logger, lspMiscellaneousFilesWorkspace, workspaceRegistrationService, _requestTelemetryLogger);

            _queue = new RequestExecutionQueue(
                logger,
                globalOptions,
                supportedLanguages,
                serverKind,
                _requestTelemetryLogger,
                _lspWorkspaceManager,
                _notificationManager);
            _queue.RequestServerShutdown += RequestExecutionQueue_Errored;

            var entryPointMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.EntryPointAsync));
            Contract.ThrowIfNull(entryPointMethod, $"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.EntryPointAsync)}");

            foreach (var metadata in _requestDispatcher.GetRegisteredMethods())
            {
                // Instead of concretely defining methods for each LSP method, we instead dynamically construct the
                // generic method info from the exported handler types.  This allows us to define multiple handlers for
                // the same method but different type parameters.  This is a key functionality to support TS external
                // access as we do not want to couple our LSP protocol version dll to theirs.
                //
                // We also do not use the StreamJsonRpc support for JToken as the rpc method parameters because we want
                // StreamJsonRpc to do the deserialization to handle streaming requests using IProgress<T>.
                var delegatingEntryPoint = new DelegatingEntryPoint(metadata.MethodName, this);

                var genericEntryPointMethod = entryPointMethod.MakeGenericMethod(metadata.RequestType, metadata.ResponseType);

                _jsonRpc.AddLocalRpcMethod(genericEntryPointMethod, delegatingEntryPoint, new JsonRpcMethodAttribute(metadata.MethodName) { UseSingleObjectParameterDeserialization = true });
            }
        }

        /// <summary>
        /// Wrapper class to hold the method and properties from the <see cref="LanguageServerTarget"/>
        /// that the method info passed to streamjsonrpc is created from.
        /// </summary>
        private class DelegatingEntryPoint
        {
            private readonly string _method;
            private readonly LanguageServerTarget _target;

            public DelegatingEntryPoint(string method, LanguageServerTarget target)
            {
                _method = method;
                _target = target;
            }

            public async Task<TResponseType?> EntryPointAsync<TRequestType, TResponseType>(TRequestType requestType, CancellationToken cancellationToken) where TRequestType : class
            {
                Contract.ThrowIfNull(_target._clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");
                var result = await _target._requestDispatcher.ExecuteRequestAsync<TRequestType, TResponseType>(
                    _method,
                    requestType,
                    _target._clientCapabilities,
                    _target._queue,
                    cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        /// <summary>
        /// Handle the LSP initialize request by storing the client capabilities and responding with the server
        /// capabilities.  The specification assures that the initialize request is sent only once.
        /// </summary>
        [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
        public Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.TraceStart("Initialize");

                Contract.ThrowIfTrue(_clientCapabilities != null, $"{nameof(InitializeAsync)} called multiple times");
                _clientCapabilities = initializeParams.Capabilities;

                return Task.FromResult(new InitializeResult
                {
                    Capabilities = _capabilitiesProvider.GetCapabilities(_clientCapabilities),
                });
            }
            finally
            {
                _logger?.TraceStop("Initialize");
            }
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public virtual Task InitializedAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities);
            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public Task ShutdownAsync(CancellationToken _)
        {
            try
            {
                _logger?.TraceStart("Shutdown");

                ShutdownImpl();

                return Task.CompletedTask;
            }
            finally
            {
                _logger?.TraceStop("Shutdown");
            }
        }

        private void ShutdownImpl()
        {
            Contract.ThrowIfTrue(_shuttingDown, "Shutdown has already been called.");

            _shuttingDown = true;

            ShutdownRequestQueue();
        }

        [JsonRpcMethod(Methods.ExitName)]
        public Task ExitAsync(CancellationToken _)
        {
            try
            {
                _logger?.TraceStart("Exit");

                ExitImpl();

                return Task.CompletedTask;
            }
            finally
            {
                _logger?.TraceStop("Exit");
            }
        }

        private void ExitImpl()
        {
            try
            {
                ShutdownRequestQueue();
                _jsonRpc.Disconnected -= JsonRpc_Disconnected;
                _jsonRpc.Dispose();
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
                // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
            }
        }

        /// <summary>
        /// Specially handle the execute workspace command method as we have to deserialize the request
        /// to figure out which <see cref="AbstractExecuteWorkspaceCommandHandler"/> actually handles it.
        /// </summary>
        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName, UseSingleObjectParameterDeserialization = true)]
        public async Task<object?> ExecuteWorkspaceCommandAsync(LSP.ExecuteCommandParams request, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");
            var requestMethod = AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(request.Command);

            var result = await _requestDispatcher.ExecuteRequestAsync<LSP.ExecuteCommandParams, object>(
                requestMethod,
                request,
                _clientCapabilities,
                _queue,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        private void ShutdownRequestQueue()
        {
            _queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
            // if the queue requested shutdown via its event, it will have already shut itself down, but this
            // won't cause any problems calling it again
            _queue.Shutdown();

            _requestTelemetryLogger.Dispose();
            _lspWorkspaceManager.Dispose();
        }

        private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
        {
            // log message and shut down
            _logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

            var message = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = e.Message
            };

            var asyncToken = _listener.BeginAsyncOperation(nameof(RequestExecutionQueue_Errored));
            _errorShutdownTask = Task.Run(async () =>
            {
                _logger?.TraceInformation("Shutting down language server.");

                await _jsonRpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, message).ConfigureAwait(false);

                ShutdownImpl();
                ExitImpl();
            }).CompletesAsyncOperation(asyncToken);
        }

        /// <summary>
        /// Cleanup the server if we encounter a json rpc disconnect so that we can be restarted later.
        /// </summary>
        private void JsonRpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            if (_shuttingDown)
            {
                // We're already in the normal shutdown -> exit path, no need to do anything.
                return;
            }

            _logger?.TraceWarning($"Encountered unexpected jsonrpc disconnect, Reason={e.Reason}, Description={e.Description}, Exception={e.Exception}");

            ShutdownImpl();
            ExitImpl();
        }

        public async ValueTask DisposeAsync()
        {
            // if the server shut down due to error, we might not have finished cleaning up
            if (_errorShutdownTask is not null)
                await _errorShutdownTask.ConfigureAwait(false);

            if (_logger is IDisposable disposableLogger)
                disposableLogger.Dispose();
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly LanguageServerTarget _server;

            internal TestAccessor(LanguageServerTarget server)
            {
                _server = server;
            }

            internal RequestExecutionQueue.TestAccessor GetQueueAccessor()
                => _server._queue.GetTestAccessor();

            internal LspWorkspaceManager.TestAccessor GetManagerAccessor()
                => _server._queue.GetTestAccessor().GetLspWorkspaceManager().GetTestAccessor();

            internal RequestDispatcher.TestAccessor GetDispatcherAccessor()
                => _server._requestDispatcher.GetTestAccessor();

            internal JsonRpc GetServerRpc() => _server._jsonRpc;

            internal bool HasShutdownStarted() => _server.HasShutdownStarted;

            internal void ShutdownServer() => _server.ShutdownImpl();

            internal void ExitServer() => _server.ExitImpl();
        }
    }
}
