// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Defines the language server to be hooked up to an <see cref="ILanguageClient"/> using StreamJsonRpc.
    /// This runs in proc as not all features provided by this server are available out of proc (e.g. some diagnostics).
    /// </summary>
    internal class InProcLanguageServer
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly JsonRpc _jsonRpc;
        private readonly LanguageServerProtocol _protocol;
        private readonly Workspace _workspace;

        private VSClientCapabilities? _clientCapabilities;

        public InProcLanguageServer(Stream inputStream, Stream outputStream, LanguageServerProtocol protocol,
            Workspace workspace, IDiagnosticService diagnosticService)
        {
            _protocol = protocol;
            _workspace = workspace;

            _jsonRpc = new JsonRpc(outputStream, inputStream, this);
            _jsonRpc.StartListening();

            _diagnosticService = diagnosticService;
            _diagnosticService.DiagnosticsUpdated += DiagnosticService_DiagnosticsUpdated;
        }

        /// <summary>
        /// Handle the LSP initialize request by storing the client capabilities
        /// and responding with the server capabilities.
        /// The specification assures that the initialize request is sent only once.
        /// </summary>
        [JsonRpcMethod(Methods.InitializeName)]
        public Task<InitializeResult> InitializeAsync(JToken input, CancellationToken cancellationToken)
        {
            // The VS LSP protocol package changed the type of 'tagSupport' from bool to an object.
            // Our version of the LSP protocol package is older and assumes that the type is bool, so deserialization fails.
            // Since we don't really read this field, just no-op the error until we can update our package references.
            // https://github.com/dotnet/roslyn/issues/40829 tracks updating this.
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    if (object.Equals(args.ErrorContext.Member, "tagSupport") && args.ErrorContext.OriginalObject.GetType() == typeof(PublishDiagnosticsSetting))
                    {
                        args.ErrorContext.Handled = true;
                    }
                }
            };
            var serializer = JsonSerializer.Create(settings);

            // InitializeParams only references ClientCapabilities, but the VS LSP client
            // sends additional VS specific capabilities, so directly deserialize them into the VSClientCapabilities
            // to avoid losing them.
            _clientCapabilities = input["capabilities"].ToObject<VSClientCapabilities>(serializer);
            return _protocol.InitializeAsync(_workspace.CurrentSolution, input.ToObject<InitializeParams>(serializer), _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public async Task InitializedAsync()
        {
            // Publish diagnostics for all open documents immediately following initialization.
            var solution = _workspace.CurrentSolution;
            var openDocuments = _workspace.GetOpenDocumentIds();
            foreach (var documentId in openDocuments)
            {
                var document = solution.GetDocument(documentId);
                if (document != null)
                {
                    await PublishDiagnosticsAsync(document).ConfigureAwait(false);
                }
            }
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public object? Shutdown(CancellationToken _) => null;

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit()
        {
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
        public Task<object> GetTextDocumentDefinitionAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return _protocol.GoToDefinitionAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public Task<object> GetTextDocumentCompletionAsync(JToken input, CancellationToken cancellationToken)
        {
            var completionParams = input.ToObject<CompletionParams>();
            return _protocol.GetCompletionsAsync(_workspace.CurrentSolution, completionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
        public Task<CompletionItem> ResolveCompletionItemAsync(JToken input, CancellationToken cancellationToken)
        {
            var completionItem = input.ToObject<CompletionItem>();
            return _protocol.ResolveCompletionItemAsync(_workspace.CurrentSolution, completionItem, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
        public Task<DocumentHighlight[]> GetTextDocumentDocumentHighlightsAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return _protocol.GetDocumentHighlightAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public Task<object[]> GetTextDocumentDocumentSymbolsAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentSymbolParams = input.ToObject<DocumentSymbolParams>();
            return _protocol.GetDocumentSymbolsAsync(_workspace.CurrentSolution, documentSymbolParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentFormattingName)]
        public Task<TextEdit[]> GetTextDocumentFormattingAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentFormattingParams = input.ToObject<DocumentFormattingParams>();
            return _protocol.FormatDocumentAsync(_workspace.CurrentSolution, documentFormattingParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName)]
        public Task<TextEdit[]> GetTextDocumentFormattingOnTypeAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentOnTypeFormattingParams = input.ToObject<DocumentOnTypeFormattingParams>();
            return _protocol.FormatDocumentOnTypeAsync(_workspace.CurrentSolution, documentOnTypeFormattingParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentImplementationName)]
        public Task<object> GetTextDocumentImplementationsAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return _protocol.FindImplementationsAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName)]
        public Task<TextEdit[]> GetTextDocumentRangeFormattingAsync(JToken input, CancellationToken cancellationToken)
        {
            var documentRangeFormattingParams = input.ToObject<DocumentRangeFormattingParams>();
            return _protocol.FormatDocumentRangeAsync(_workspace.CurrentSolution, documentRangeFormattingParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
        public async Task<SignatureHelp> GetTextDocumentSignatureHelpAsync(JToken input, CancellationToken cancellationToken)
        {
            var textDocumentPositionParams = input.ToObject<TextDocumentPositionParams>();
            return await _protocol.GetSignatureHelpAsync(_workspace.CurrentSolution, textDocumentPositionParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName)]
        public async Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(JToken input, CancellationToken cancellationToken)
        {
            var workspaceSymbolParams = input.ToObject<WorkspaceSymbolParams>();
            return await _protocol.GetWorkspaceSymbolsAsync(_workspace.CurrentSolution, workspaceSymbolParams, _clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void DiagnosticService_DiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            // Since this is an async void method, exceptions here will crash the host VS. We catch exceptions here to make sure that we don't crash the host since
            // the worst outcome here is that guests may not see all diagnostics.
            try
            {
                // LSP doesnt support diagnostics without a document. So if we get project level diagnostics without a document, ignore them.
                if (e.DocumentId != null && e.Solution != null)
                {
                    var document = e.Solution.GetDocument(e.DocumentId);
                    if (document == null || document.FilePath == null)
                    {
                        return;
                    }

                    // Only publish document diagnostics for the languages this provider supports.
                    if (document.Project.Language != LanguageNames.CSharp && document.Project.Language != LanguageNames.VisualBasic)
                    {
                        return;
                    }

                    // LSP does not currently support publishing diagnostics incrememntally, so we re-publish all diagnostics.
                    await PublishDiagnosticsAsync(document).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
            {
            }
        }

        private async Task PublishDiagnosticsAsync(Document document)
        {
            var diagnostics = await GetDiagnosticsAsync(document, CancellationToken.None).ConfigureAwait(false);
            var publishDiagnosticsParams = new PublishDiagnosticParams { Diagnostics = diagnostics, Uri = document.GetURI() };
            await _jsonRpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, publishDiagnosticsParams).ConfigureAwait(false);
        }

        private async Task<LanguageServer.Protocol.Diagnostic[]> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var diagnostics = _diagnosticService.GetDiagnostics(document.Project.Solution.Workspace, document.Project.Id, document.Id, null, false, cancellationToken);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return diagnostics.Select(diagnostic => new LanguageServer.Protocol.Diagnostic
            {
                Code = diagnostic.Id,
                Message = diagnostic.Message,
                Severity = ProtocolConversions.DiagnosticSeverityToLspDiagnositcSeverity(diagnostic.Severity),
                Range = ProtocolConversions.TextSpanToRange(DiagnosticData.GetExistingOrCalculatedTextSpan(diagnostic.DataLocation, text), text),
                // Only the unnecessary diagnostic tag is currently supported via LSP.
                Tags = diagnostic.CustomTags.Contains("Unnecessary") ? new DiagnosticTag[] { DiagnosticTag.Unnecessary } : Array.Empty<DiagnosticTag>()
            }).ToArray();
        }
    }
}
