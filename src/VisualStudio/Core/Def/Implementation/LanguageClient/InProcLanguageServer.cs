// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    /// <summary>
    /// Defines the language server to be hooked up to an <see cref="ILanguageClient"/> using StreamJsonRpc.  This runs
    /// in proc as not all features provided by this server are available out of proc (e.g. some diagnostics).
    /// </summary>
    internal class InProcLanguageServer
    {
        private readonly IAsynchronousOperationListener _listener;
        private readonly string? _clientName;
        private readonly JsonRpc _jsonRpc;
        private readonly AbstractInProcLanguageClient _languageClient;
        private readonly AbstractRequestHandlerProvider _requestHandlerProvider;
        private readonly Workspace _workspace;
        private readonly RequestExecutionQueue _queue;

        private VSClientCapabilities? _clientCapabilities;
        private bool _shuttingDown;

        public InProcLanguageServer(
            AbstractInProcLanguageClient languageClient,
            Stream inputStream,
            Stream outputStream,
            AbstractRequestHandlerProvider requestHandlerProvider,
            Workspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspSolutionProvider solutionProvider,
            string? clientName)
        {
            _languageClient = languageClient;
            _requestHandlerProvider = requestHandlerProvider;
            _workspace = workspace;

            var jsonMessageFormatter = new JsonMessageFormatter();
            jsonMessageFormatter.JsonSerializer.Converters.Add(new VSExtensionConverter<TextDocumentIdentifier, VSTextDocumentIdentifier>());
            jsonMessageFormatter.JsonSerializer.Converters.Add(new VSExtensionConverter<ClientCapabilities, VSClientCapabilities>());

            _jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, jsonMessageFormatter));
            _jsonRpc.AddLocalRpcTarget(this);
            _jsonRpc.StartListening();

            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            _clientName = clientName;

            _queue = new RequestExecutionQueue(solutionProvider);
            _queue.RequestServerShutdown += RequestExecutionQueue_Errored;
        }

        public bool Running => !_shuttingDown && !_jsonRpc.IsDisposed;

        /// <summary>
        /// Handle the LSP initialize request by storing the client capabilities and responding with the server
        /// capabilities.  The specification assures that the initialize request is sent only once.
        /// </summary>
        [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
        public Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(_clientCapabilities != null, $"{nameof(InitializeAsync)} called multiple times");
            _clientCapabilities = (VSClientCapabilities)initializeParams.Capabilities;
            return Task.FromResult(new InitializeResult
            {
                Capabilities = _languageClient.GetCapabilities(),
            });
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public Task ShutdownAsync(CancellationToken _)
        {
            Contract.ThrowIfTrue(_shuttingDown, "Shutdown has already been called.");

            _shuttingDown = true;

            ShutdownRequestQueue();

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public Task ExitAsync(CancellationToken _)
        {
            Contract.ThrowIfFalse(_shuttingDown, "Shutdown has not been called yet.");

            try
            {
                _jsonRpc.Dispose();
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
                // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod(MSLSPMethods.DocumentPullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<DiagnosticReport[]?> GetDocumentPullDiagnosticsAsync(DocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]?>(
                _queue, MSLSPMethods.DocumentPullDiagnosticName,
                diagnosticsParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.WorkspacePullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceDiagnosticReport[]?> GetWorkspacePullDiagnosticsAsync(WorkspaceDocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport[]?>(
                _queue, MSLSPMethods.WorkspacePullDiagnosticName,
                diagnosticsParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentDefinitionAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(_queue, Methods.TextDocumentDefinitionName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceEdit> GetTextDocumentRenameAsync(RenameParams renameParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<RenameParams, WorkspaceEdit>(_queue, Methods.TextDocumentRenameName,
                renameParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSReferenceItem[]?> GetTextDocumentReferencesAsync(ReferenceParams referencesParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<ReferenceParams, VSReferenceItem[]?>(_queue, Methods.TextDocumentReferencesName,
                referencesParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSCodeAction[]> GetTextDocumentCodeActionsAsync(CodeActionParams codeActionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<CodeActionParams, VSCodeAction[]>(_queue, Methods.TextDocumentCodeActionName, codeActionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.TextDocumentCodeActionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSCodeAction> ResolveCodeActionAsync(VSCodeAction vsCodeAction, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<VSCodeAction, VSCodeAction>(_queue, MSLSPMethods.TextDocumentCodeActionResolveName,
                vsCodeAction, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SumType<CompletionList, CompletionItem[]>> GetTextDocumentCompletionAsync(CompletionParams completionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            // Convert to sumtype before reporting to work around https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107698
            return await _requestHandlerProvider.ExecuteRequestAsync<CompletionParams, CompletionList>(_queue, Methods.TextDocumentCompletionName,
                completionParams, _clientCapabilities, _clientName, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<CompletionItem> ResolveCompletionItemAsync(CompletionItem completionItem, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<CompletionItem, CompletionItem>(_queue, Methods.TextDocumentCompletionResolveName, completionItem, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentFoldingRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<FoldingRange[]> GetTextDocumentFoldingRangeAsync(FoldingRangeParams textDocumentFoldingRangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<FoldingRangeParams, FoldingRange[]>(_queue, Methods.TextDocumentFoldingRangeName, textDocumentFoldingRangeParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentHighlight[]> GetTextDocumentDocumentHighlightsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, DocumentHighlight[]>(_queue, Methods.TextDocumentDocumentHighlightName, textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName, UseSingleObjectParameterDeserialization = true)]
        public Task<Hover?> GetTextDocumentDocumentHoverAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, Hover?>(_queue, Methods.TextDocumentHoverName, textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<object[]> GetTextDocumentDocumentSymbolsAsync(DocumentSymbolParams documentSymbolParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentSymbolParams, object[]>(_queue, Methods.TextDocumentDocumentSymbolName, documentSymbolParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingAsync(DocumentFormattingParams documentFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentFormattingName, documentFormattingParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingOnTypeAsync(DocumentOnTypeFormattingParams documentOnTypeFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentOnTypeFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentOnTypeFormattingName, documentOnTypeFormattingParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentImplementationName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentImplementationsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(_queue, Methods.TextDocumentImplementationName, textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentRangeFormattingAsync(DocumentRangeFormattingParams documentRangeFormattingParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentRangeFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentRangeFormattingName, documentRangeFormattingParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName, UseSingleObjectParameterDeserialization = true)]
        public Task<SignatureHelp> GetTextDocumentSignatureHelpAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, SignatureHelp>(_queue, Methods.TextDocumentSignatureHelpName, textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName, UseSingleObjectParameterDeserialization = true)]
        public Task<object> ExecuteWorkspaceCommandAsync(ExecuteCommandParams executeCommandParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<ExecuteCommandParams, object>(_queue, Methods.WorkspaceExecuteCommandName, executeCommandParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(WorkspaceSymbolParams workspaceSymbolParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<WorkspaceSymbolParams, SymbolInformation[]>(_queue, Methods.WorkspaceSymbolName, workspaceSymbolParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.ProjectContextsName, UseSingleObjectParameterDeserialization = true)]
        public Task<ActiveProjectContexts?> GetProjectContextsAsync(GetTextDocumentWithContextParams textDocumentWithContextParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<GetTextDocumentWithContextParams, ActiveProjectContexts?>(_queue, MSLSPMethods.ProjectContextsName,
                textDocumentWithContextParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensParams, SemanticTokens>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensName,
                semanticTokensParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensEditsName, UseSingleObjectParameterDeserialization = true)]
        public Task<SumType<SemanticTokens, SemanticTokensEdits>> GetTextDocumentSemanticTokensEditsAsync(SemanticTokensEditsParams semanticTokensEditsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensEditsParams, SumType<SemanticTokens, SemanticTokensEdits>>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                semanticTokensEditsParams, _clientCapabilities, _clientName, cancellationToken);
        }

        // Note: Since a range request is always received in conjunction with a whole document request, we don't need to cache range results.
        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams semanticTokensRangeParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensRangeParams, SemanticTokens>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensRangeName,
                semanticTokensRangeParams, _clientCapabilities, _clientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnAutoInsertResponseItem[]> GetDocumentOnAutoInsertAsync(DocumentOnAutoInsertParams autoInsertParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return _requestHandlerProvider.ExecuteRequestAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem[]>(_queue, MSLSPMethods.OnAutoInsertName,
                autoInsertParams, _clientCapabilities, _clientName, cancellationToken);
        }

        private void ShutdownRequestQueue()
        {
            _queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
            // if the queue requested shutdown via its event, it will have already shut itself down, but this
            // won't cause any problems calling it again
            _queue.Shutdown();
        }

        private void RequestExecutionQueue_Errored(object sender, RequestShutdownEventArgs e)
        {
            // log message and shut down

            var message = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = e.Message
            };

            var asyncToken = _listener.BeginAsyncOperation(nameof(RequestExecutionQueue_Errored));
            Task.Run(async () =>
            {
                await _jsonRpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, message).ConfigureAwait(false);

                // The "default" here is the cancellation token, which these methods don't use, hence the discard name
                await ShutdownAsync(_: default).ConfigureAwait(false);
                await ExitAsync(_: default).ConfigureAwait(false);
            }).CompletesAsyncOperation(asyncToken);
        }
    }
}
