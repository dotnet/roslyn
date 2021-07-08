﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            string? clientName,
            string userVisibleServerName,
            string telemetryServerTypeName)
        {
            RequestDispatcher = requestDispatcherFactory.CreateRequestDispatcher();

            _capabilitiesProvider = capabilitiesProvider;
            WorkspaceRegistrationService = workspaceRegistrationService;
            Logger = logger;

            JsonRpc = jsonRpc;
            JsonRpc.AddLocalRpcTarget(this);

            Listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            ClientName = clientName;

            _userVisibleServerName = userVisibleServerName;
            TelemetryServerName = telemetryServerTypeName;

            Queue = new RequestExecutionQueue(logger, workspaceRegistrationService, userVisibleServerName, TelemetryServerName);
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
        public Task<ReferenceItem[]?> GetTextDocumentReferencesAsync(ReferenceParams referencesParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<ReferenceParams, ReferenceItem[]?>(Queue, Methods.TextDocumentReferencesName,
                referencesParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName, UseSingleObjectParameterDeserialization = true)]
        public Task<CodeAction[]> GetTextDocumentCodeActionsAsync(CodeActionParams codeActionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(Queue, Methods.TextDocumentCodeActionName, codeActionParams, _clientCapabilities, ClientName, cancellationToken);
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

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensParams, SemanticTokens>(Queue, SemanticTokensMethods.TextDocumentSemanticTokensName,
                semanticTokensParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensEditsName, UseSingleObjectParameterDeserialization = true)]
        public Task<SumType<SemanticTokens, SemanticTokensEdits>> GetTextDocumentSemanticTokensEditsAsync(SemanticTokensEditsParams semanticTokensEditsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensEditsParams, SumType<SemanticTokens, SemanticTokensEdits>>(Queue, SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                semanticTokensEditsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        // Note: Since a range request is always received in conjunction with a whole document request, we don't need to cache range results.
        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams semanticTokensRangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<SemanticTokensRangeParams, SemanticTokens>(Queue, SemanticTokensMethods.TextDocumentSemanticTokensRangeName,
                semanticTokensRangeParams, _clientCapabilities, ClientName, cancellationToken);
        }

        // Temporary workaround to specify the actual public LSP method name until the LSP protocol package updates.
        // Tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1249055
        [JsonRpcMethod("textDocument/semanticTokens/full", UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensPublicAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken)
            => GetTextDocumentSemanticTokensAsync(semanticTokensParams, cancellationToken);

        // Temporary workaround to specify the actual public LSP method name until the LSP protocol package updates.
        // Tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1249055
        [JsonRpcMethod("textDocument/semanticTokens/full/delta", UseSingleObjectParameterDeserialization = true)]
        public Task<SumType<SemanticTokens, SemanticTokensEdits>> GetTextDocumentSemanticTokensEditsPublicAsync(SemanticTokensEditsParams semanticTokensEditsParams, CancellationToken cancellationToken)
            => GetTextDocumentSemanticTokensEditsAsync(semanticTokensEditsParams, cancellationToken);

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
