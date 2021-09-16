// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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
        protected readonly ILspWorkspaceRegistrationService WorkspaceRegistrationService;
        protected readonly IAsynchronousOperationListener Listener;
        protected readonly ILspLogger Logger;
        protected readonly string? ClientName;

        /// <summary>
        /// Server name used when sending error messages to the client.
        /// </summary>
        private readonly string _userVisibleServerName;

        /// <summary>
        /// Server name used when capturing error telemetry.
        /// </summary>
        protected readonly string TelemetryServerName;

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
            ILspWorkspaceRegistrationService workspaceRegistrationService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            ImmutableArray<string> supportedLanguages,
            string? clientName,
            string userVisibleServerName,
            string telemetryServerTypeName)
        {
            GlobalOptions = globalOptions;
            RequestDispatcher = requestDispatcherFactory.CreateRequestDispatcher(supportedLanguages);

            _capabilitiesProvider = capabilitiesProvider;
            WorkspaceRegistrationService = workspaceRegistrationService;
            Logger = logger;

            JsonRpc = jsonRpc;
            JsonRpc.AddLocalRpcTarget(this);
            JsonRpc.Disconnected += JsonRpc_Disconnected;

            Listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            ClientName = clientName;

            _userVisibleServerName = userVisibleServerName;
            TelemetryServerName = telemetryServerTypeName;

            Queue = new RequestExecutionQueue(logger, workspaceRegistrationService, globalOptions, supportedLanguages, userVisibleServerName, TelemetryServerName);
            Queue.RequestServerShutdown += RequestExecutionQueue_Errored;
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

        protected virtual void ShutdownImpl()
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

        private void ExitImpl()
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

        [JsonRpcMethod(Methods.TextDocumentDefinitionName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentDefinitionAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(Queue, Methods.TextDocumentDefinitionName,
                textDocumentPositionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceEdit> GetTextDocumentRenameAsync(RenameParams renameParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<RenameParams, WorkspaceEdit>(Queue, Methods.TextDocumentRenameName,
                renameParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalReferenceItem[]?> GetTextDocumentReferencesAsync(ReferenceParams referencesParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<ReferenceParams, VSInternalReferenceItem[]?>(Queue, Methods.TextDocumentReferencesName,
                referencesParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName, UseSingleObjectParameterDeserialization = true)]
        public Task<CodeAction[]> GetTextDocumentCodeActionsAsync(CodeActionParams codeActionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(Queue, Methods.TextDocumentCodeActionName, codeActionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.CodeActionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<CodeAction> ResolveCodeActionAsync(CodeAction codeAction, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<CodeAction, CodeAction>(Queue, Methods.CodeActionResolveName,
                codeAction, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SumType<CompletionList, CompletionItem[]>> GetTextDocumentCompletionAsync(CompletionParams completionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            // Convert to sumtype before reporting to work around https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107698
            return await RequestDispatcher.ExecuteRequestAsync<CompletionParams, CompletionList>(Queue, Methods.TextDocumentCompletionName,
                completionParams, _clientCapabilities, ClientName, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<CompletionItem> ResolveCompletionItemAsync(CompletionItem completionItem, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<CompletionItem, CompletionItem>(Queue, Methods.TextDocumentCompletionResolveName, completionItem, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentFoldingRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<FoldingRange[]> GetTextDocumentFoldingRangeAsync(FoldingRangeParams textDocumentFoldingRangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<FoldingRangeParams, FoldingRange[]>(Queue, Methods.TextDocumentFoldingRangeName, textDocumentFoldingRangeParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentHighlight[]> GetTextDocumentDocumentHighlightsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<TextDocumentPositionParams, DocumentHighlight[]>(Queue, Methods.TextDocumentDocumentHighlightName, textDocumentPositionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName, UseSingleObjectParameterDeserialization = true)]
        public Task<Hover?> GetTextDocumentDocumentHoverAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<TextDocumentPositionParams, Hover?>(Queue, Methods.TextDocumentHoverName, textDocumentPositionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<object[]> GetTextDocumentDocumentSymbolsAsync(DocumentSymbolParams documentSymbolParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentSymbolParams, object[]>(Queue, Methods.TextDocumentDocumentSymbolName, documentSymbolParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingAsync(DocumentFormattingParams documentFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentFormattingParams, TextEdit[]>(Queue, Methods.TextDocumentFormattingName, documentFormattingParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingOnTypeAsync(DocumentOnTypeFormattingParams documentOnTypeFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentOnTypeFormattingParams, TextEdit[]>(Queue, Methods.TextDocumentOnTypeFormattingName, documentOnTypeFormattingParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentImplementationName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentImplementationsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(Queue, Methods.TextDocumentImplementationName, textDocumentPositionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentRangeFormattingAsync(DocumentRangeFormattingParams documentRangeFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentRangeFormattingParams, TextEdit[]>(Queue, Methods.TextDocumentRangeFormattingName, documentRangeFormattingParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.SignatureHelp?> GetTextDocumentSignatureHelpAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<TextDocumentPositionParams, LSP.SignatureHelp?>(Queue, Methods.TextDocumentSignatureHelpName, textDocumentPositionParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName, UseSingleObjectParameterDeserialization = true)]
        public Task<object> ExecuteWorkspaceCommandAsync(ExecuteCommandParams executeCommandParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<ExecuteCommandParams, object>(Queue, Methods.WorkspaceExecuteCommandName, executeCommandParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]?> GetWorkspaceSymbolsAsync(WorkspaceSymbolParams workspaceSymbolParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<WorkspaceSymbolParams, SymbolInformation[]?>(Queue, Methods.WorkspaceSymbolName, workspaceSymbolParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSemanticTokensFullName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensParams, SemanticTokens>(Queue, Methods.TextDocumentSemanticTokensFullName,
                semanticTokensParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSemanticTokensFullDeltaName, UseSingleObjectParameterDeserialization = true)]
        public Task<SumType<SemanticTokens, SemanticTokensDelta>> GetTextDocumentSemanticTokensEditsAsync(SemanticTokensDeltaParams semanticTokensEditsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensDeltaParams, SumType<SemanticTokens, SemanticTokensDelta>>(Queue, Methods.TextDocumentSemanticTokensFullDeltaName,
                semanticTokensEditsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        // Note: Since a range request is always received in conjunction with a whole document request, we don't need to cache range results.
        [JsonRpcMethod(Methods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams semanticTokensRangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensRangeParams, SemanticTokens>(Queue, Methods.TextDocumentSemanticTokensRangeName,
                semanticTokensRangeParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<object> HandleDocumentDidChangeAsync(DidChangeTextDocumentParams didChangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DidChangeTextDocumentParams, object>(Queue, Methods.TextDocumentDidChangeName,
                didChangeParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName, UseSingleObjectParameterDeserialization = true)]
        public Task<object?> HandleDocumentDidOpenAsync(DidOpenTextDocumentParams didOpenParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DidOpenTextDocumentParams, object?>(Queue, Methods.TextDocumentDidOpenName,
                didOpenParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName, UseSingleObjectParameterDeserialization = true)]
        public Task<object?> HandleDocumentDidCloseAsync(DidCloseTextDocumentParams didCloseParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DidCloseTextDocumentParams, object?>(Queue, Methods.TextDocumentDidCloseName,
                didCloseParams, _clientCapabilities, ClientName, cancellationToken);
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

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this.Queue);

        internal readonly struct TestAccessor
        {
            private readonly RequestExecutionQueue _queue;

            public TestAccessor(RequestExecutionQueue queue)
                => _queue = queue;

            public RequestExecutionQueue.TestAccessor GetQueueAccessor()
                => _queue.GetTestAccessor();
        }
    }
}
