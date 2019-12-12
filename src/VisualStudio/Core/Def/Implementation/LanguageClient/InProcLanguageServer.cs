// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal class InProcLanguageServer
    {
        private readonly JsonRpc _jsonRpc;
        private readonly LanguageServerProtocol _protocol;
        private readonly Workspace _workspace;

        private VSClientCapabilities? _clientCapabilities;

        public InProcLanguageServer(Stream inputStream, Stream outputStream, LanguageServerProtocol protocol, Workspace workspace)
        {
            this._protocol = protocol;
            this._workspace = workspace;

            this._jsonRpc = new JsonRpc(outputStream, inputStream, this);
            this._jsonRpc.StartListening();
        }

        /// <summary>
        /// Handle the LSP initialize request by storing the client capabilities
        /// and responding with the server capabilities.
        /// The specification assures that the initialize request is sent only once.
        /// </summary>
        [JsonRpcMethod(Methods.InitializeName)]
        public InitializeResult Initialize(JToken input)
        {
            // InitializeParams only references ClientCapabilities, but the VS LSP client
            // sends additional VS specific capabilities, so directly deserialize them into the VSClientCapabilities
            // to avoid losing them.
            this._clientCapabilities = input["capabilities"].ToObject<VSClientCapabilities>();

            return new InitializeResult
            {
                Capabilities = new VSServerCapabilities
                {
                    DocumentHighlightProvider = true,
                    DocumentSymbolProvider = true,
                    DocumentFormattingProvider = true,
                    DocumentRangeFormattingProvider = true,
                    DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                    DefinitionProvider = true,
                    CompletionProvider = new CompletionOptions { ResolveProvider = true, TriggerCharacters = new[] { "." } },
                    SignatureHelpProvider = new SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } },
                    WorkspaceSymbolProvider = true,
                }
            };
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public Task Initialized() => Task.CompletedTask;

        [JsonRpcMethod(Methods.ShutdownName)]
        public object? Shutdown(CancellationToken _) => null;

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit()
        {
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
        public async Task<object> GetTextDocumentDefinitionAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return await this._protocol.GoToDefinitionAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public async Task<object> GetTextDocumentCompletionAsync(JToken input, CancellationToken cancellationToken)
        {
            var completionParams = input.ToObject<CompletionParams>();
            return await this._protocol.GetCompletionsAsync(_workspace.CurrentSolution, completionParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
        public async Task<object> ResolveCompletionItemAsync(JToken input, CancellationToken cancellationToken)
        {
            var completionItem = input.ToObject<CompletionItem>();
            return await this._protocol.ResolveCompletionItemAsync(_workspace.CurrentSolution, completionItem, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
        public async Task<DocumentHighlight[]> GetTextDocumentDocumentHighlightsAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return await this._protocol.GetDocumentHighlightAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public async Task<object[]> GetTextDocumentDocumentSymbolsAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentSymbolParams = input.ToObject<DocumentSymbolParams>();
            return await this._protocol.GetDocumentSymbolsAsync(_workspace.CurrentSolution, documentSymbolParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentFormattingName)]
        public async Task<TextEdit[]> GetTextDocumentFormattingAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentFormattingParams = input.ToObject<DocumentFormattingParams>();
            return await this._protocol.FormatDocumentAsync(_workspace.CurrentSolution, documentFormattingParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName)]
        public async Task<TextEdit[]> GetTextDocumentFormattingOnTypeAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentOnTypeFormattingParams = input.ToObject<DocumentOnTypeFormattingParams>();
            return await this._protocol.FormatDocumentOnTypeAsync(_workspace.CurrentSolution, documentOnTypeFormattingParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName)]
        public async Task<TextEdit[]> GetTextDocumentRangeFormattingAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentRangeFormattingParams = input.ToObject<DocumentRangeFormattingParams>();
            return await this._protocol.FormatDocumentRangeAsync(_workspace.CurrentSolution, documentRangeFormattingParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
        public async Task<SignatureHelp> GetTextDocumentSignatureHelpAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return await this._protocol.GetSignatureHelpAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName)]
        public async Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(JToken input, CancellationToken cancellationToken)
        {
            var workspaceSymbolParams = input.ToObject<WorkspaceSymbolParams>();
            return await this._protocol.GetWorkspaceSymbolsAsync(_workspace.CurrentSolution, workspaceSymbolParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }
    }
}
