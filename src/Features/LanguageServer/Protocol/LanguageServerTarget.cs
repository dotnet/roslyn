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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class LanguageServerTarget : ILanguageServerTarget
    {
        private readonly ICapabilitiesProvider _capabilitiesProvider;

        protected readonly IGlobalOptionService GlobalOptions;
        protected readonly JsonRpc JsonRpc;
        protected readonly RequestDispatcher RequestDispatcher;
        protected readonly RequestExecutionQueue Queue;
        protected readonly LspWorkspaceRegistrationService WorkspaceRegistrationService;
        protected readonly IAsynchronousOperationListener Listener;
        protected readonly ILspLogger Logger;
        protected readonly string? ClientName;

        // Set on first LSP initialize request.
        protected ClientCapabilities? _clientCapabilities;

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
            string? clientName,
            WellKnownLspServerKinds serverKind)
        {
            GlobalOptions = globalOptions;
            RequestDispatcher = requestDispatcherFactory.CreateRequestDispatcher(serverKind);

            _capabilitiesProvider = capabilitiesProvider;
            WorkspaceRegistrationService = workspaceRegistrationService;
            Logger = logger;

            JsonRpc = jsonRpc;
            JsonRpc.AddLocalRpcTarget(this);
            JsonRpc.Disconnected += JsonRpc_Disconnected;

            Listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            ClientName = clientName;

            Queue = new RequestExecutionQueue(
                logger,
                workspaceRegistrationService,
                lspMiscellaneousFilesWorkspace,
                globalOptions,
                supportedLanguages,
                serverKind);
            Queue.RequestServerShutdown += RequestExecutionQueue_Errored;

            foreach (var metadata in RequestDispatcher.GetRegisteredMethods())
            {
                // Instead of concretely defining methods for each LSP method, we instead dynamically construct
                // the generic method info from the exported handler types.  This allows us to define multiple handlers for the same method
                // but different type parameters.  This is a key functionality to support TS external access as we do not want to couple
                // our LSP protocol version dll to theirs.
                //
                // We also do not use the StreamJsonRpc support for JToken as the rpc method parameters because we want
                // StreamJsonRpc to do the deserialization to handle streaming requests using IProgress<T>.
                var delegatingEntryPoint = new DelegatingEntryPoint(metadata.MethodName, this);

                var entryPointMethod = delegatingEntryPoint.GetType().GetMethod(nameof(DelegatingEntryPoint.EntryPointAsync));
                entryPointMethod = entryPointMethod!.MakeGenericMethod(metadata.RequestType, metadata.ResponseType);

                JsonRpc.AddLocalRpcMethod(entryPointMethod, delegatingEntryPoint, new JsonRpcMethodAttribute(metadata.MethodName) { UseSingleObjectParameterDeserialization = true });
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
                var result = await _target.RequestDispatcher.ExecuteRequestAsync<TRequestType, TResponseType>(
                    _method,
                    requestType,
                    _target._clientCapabilities,
                    _target.ClientName,
                    _target.Queue,
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
                Logger?.TraceStart("Initialize");

                Contract.ThrowIfTrue(_clientCapabilities != null, $"{nameof(InitializeAsync)} called multiple times");
                _clientCapabilities = initializeParams.Capabilities;
                return Task.FromResult(new InitializeResult
                {
                    Capabilities = _capabilitiesProvider.GetCapabilities(_clientCapabilities),
                });
            }
            finally
            {
                Logger?.TraceStop("Initialize");
            }
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public virtual Task InitializedAsync()
        {
            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public Task ShutdownAsync(CancellationToken _)
        {
            try
            {
                Logger?.TraceStart("Shutdown");

                ShutdownImpl();

                return Task.CompletedTask;
            }
            finally
            {
                Logger?.TraceStop("Shutdown");
            }
        }

        protected void ShutdownImpl()
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
                Logger?.TraceStart("Exit");

                ExitImpl();

                return Task.CompletedTask;
            }
            finally
            {
                Logger?.TraceStop("Exit");
            }
        }

        protected void ExitImpl()
        {
            try
            {
                ShutdownRequestQueue();
                JsonRpc.Disconnected -= JsonRpc_Disconnected;
                JsonRpc.Dispose();
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

            var result = await RequestDispatcher.ExecuteRequestAsync<LSP.ExecuteCommandParams, object>(
                requestMethod,
                request,
                _clientCapabilities,
                ClientName,
                Queue,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        private void ShutdownRequestQueue()
        {
            Queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
            // if the queue requested shutdown via its event, it will have already shut itself down, but this
            // won't cause any problems calling it again
            Queue.Shutdown();
        }

        private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
        {
            // log message and shut down
            Logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

            var message = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = e.Message
            };

            var asyncToken = Listener.BeginAsyncOperation(nameof(RequestExecutionQueue_Errored));
            _errorShutdownTask = Task.Run(async () =>
            {
                Logger?.TraceInformation("Shutting down language server.");

                await JsonRpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, message).ConfigureAwait(false);

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

            Logger?.TraceWarning($"Encountered unexpected jsonrpc disconnect, Reason={e.Reason}, Description={e.Description}, Exception={e.Exception}");

            ShutdownImpl();
            ExitImpl();
        }

        public async ValueTask DisposeAsync()
        {
            // if the server shut down due to error, we might not have finished cleaning up
            if (_errorShutdownTask is not null)
                await _errorShutdownTask.ConfigureAwait(false);

            if (Logger is IDisposable disposableLogger)
                disposableLogger.Dispose();
        }
    }
}
