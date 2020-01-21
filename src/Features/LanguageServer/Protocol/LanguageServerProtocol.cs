// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Implements Language Server Protocol
    /// TODO - Make this public when we're ready.
    /// </summary>
    [Shared]
    [Export(typeof(LanguageServerProtocol))]
    internal sealed class LanguageServerProtocol
    {
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;

        [ImportingConstructor]
        public LanguageServerProtocol([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlers);
        }

        private static ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> CreateMethodToHandlerMap(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, Lazy<IRequestHandler, IRequestHandlerMetadata>>();
            foreach (var lazyHandler in requestHandlers)
            {
                requestHandlerDictionary.Add(lazyHandler.Metadata.MethodName, lazyHandler);
            }

            return requestHandlerDictionary.ToImmutable();
        }

        private Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, Solution solution, RequestType request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken) where RequestType : class
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(request);
            if (string.IsNullOrEmpty(methodName))
            {
                Contract.Fail("Invalid method name");
            }

            var handler = (IRequestHandler<RequestType, ResponseType>)_requestHandlers[methodName]?.Value;
            Contract.ThrowIfNull(handler, string.Format("Request handler not found for method {0}", methodName));

            return handler.HandleRequestAsync(solution, request, clientCapabilities, cancellationToken);
        }

        /// <summary>
        /// Answers an execute workspace command request by handling the specified command name.
        /// https://microsoft.github.io/language-server-protocol/specification#workspace_executeCommand
        /// </summary>
        /// <param name="solution">the solution relevant to the workspace.</param>
        /// <param name="request">the request command name and arguments.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>any or null.</returns>
        public Task<object> ExecuteWorkspaceCommandAsync(Solution solution, LSP.ExecuteCommandParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.ExecuteCommandParams, object>(LSP.Methods.WorkspaceExecuteCommandName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers an implementation request by returning the implementation location(s) of a given symbol.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_implementation
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document symbol location.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location(s) of the implementations of the symbol.</returns>
        public Task<object> FindImplementationsAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, object>(LSP.Methods.TextDocumentImplementationName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a format document request to format the entire document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_formatting
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document and formatting options.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the text edits describing the document modifications.</returns>
        public Task<LSP.TextEdit[]> FormatDocumentAsync(Solution solution, LSP.DocumentFormattingParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.DocumentFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentFormattingName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a format document on type request to format parts of the document during typing.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_onTypeFormatting
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document, formatting options, and typing information.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the text edits describing the document modifications.</returns>
        public Task<LSP.TextEdit[]> FormatDocumentOnTypeAsync(Solution solution, LSP.DocumentOnTypeFormattingParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.DocumentOnTypeFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentOnTypeFormattingName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a format document range request to format a specific range in the document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_rangeFormatting
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document, formatting options, and range to format.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the text edits describing the document modifications.</returns>
        public Task<LSP.TextEdit[]> FormatDocumentRangeAsync(Solution solution, LSP.DocumentRangeFormattingParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.DocumentRangeFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentRangeFormattingName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a code action request by returning the code actions for the document and range.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_codeAction
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the document and range to get code actions for.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of commands representing code actions.</returns>
        public Task<object[]> GetCodeActionsAsync(Solution solution, LSP.CodeActionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.CodeActionParams, object[]>(LSP.Methods.TextDocumentCodeActionName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a completion request by returning the valid completions at the location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_completion
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the document position and completion context.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of completions.</returns>
        public Task<object> GetCompletionsAsync(Solution solution, LSP.CompletionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.CompletionParams, object>(LSP.Methods.TextDocumentCompletionName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a document highlights request by returning the highlights for a given document location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentHighlight
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document location.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the highlights in the document for the given document location.</returns>
        public Task<LSP.DocumentHighlight[]> GetDocumentHighlightAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]>(LSP.Methods.TextDocumentDocumentHighlightName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a document symbols request by returning a list of symbols in the document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentSymbol
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the document to get symbols from.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the document.</returns>
        public Task<object[]> GetDocumentSymbolsAsync(Solution solution, LSP.DocumentSymbolParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.DocumentSymbolParams, object[]>(LSP.Methods.TextDocumentDocumentSymbolName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a folding range request by returning all folding ranges in a given document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_foldingRange
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of folding ranges in the document.</returns>
        public Task<LSP.FoldingRange[]> GetFoldingRangeAsync(Solution solution, LSP.FoldingRangeParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.FoldingRangeParams, LSP.FoldingRange[]>(LSP.Methods.TextDocumentFoldingRangeName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a Hover request by returning the quick info at the requested location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
        /// </summary>
        /// <param name="solution">the solution containing any documents in the request.</param>
        /// <param name="request">the hover requesst.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the Hover using MarkupContent.</returns>
        public Task<LSP.Hover> GetHoverAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a signature help request to get signature information at a given cursor position.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_signatureHelp
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document position.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the signature help at a given location.</returns>
        public Task<LSP.SignatureHelp> GetSignatureHelpAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.SignatureHelp>(LSP.Methods.TextDocumentSignatureHelpName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a workspace symbols request by providing a list of symbols found in a given workspace.
        /// https://microsoft.github.io/language-server-protocol/specification#workspace_symbol
        /// </summary>
        /// <param name="solution">the current solution.</param>
        /// <param name="request">the workspace request with the query to invoke.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the workspace.</returns>
        public Task<LSP.SymbolInformation[]> GetWorkspaceSymbolsAsync(Solution solution, LSP.WorkspaceSymbolParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.WorkspaceSymbolParams, LSP.SymbolInformation[]>(LSP.Methods.WorkspaceSymbolName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a goto definition request by returning the location for a given symbol definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_definition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the symbol to go to.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location(s) of a given symbol.</returns>
        public Task<object> GoToDefinitionAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, object>(LSP.Methods.TextDocumentDefinitionName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a goto type definition request by returning the location of a given type definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_typeDefinition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the type to go to.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location of a type definition.</returns>
        public Task<LSP.Location[]> GoToTypeDefinitionAsync(Solution solution, LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentTypeDefinitionName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers an initialize request by returning the server capabilities.
        /// https://microsoft.github.io/language-server-protocol/specification#initialize
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the initialize parameters.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the server capabilities.</returns>
        public Task<LSP.InitializeResult> InitializeAsync(Solution solution, LSP.InitializeParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.InitializeParams, LSP.InitializeResult>(LSP.Methods.InitializeName, solution, request, clientCapabilities, cancellationToken);

        /// <summary>
        /// Answers a request to resolve a completion item.
        /// https://microsoft.github.io/language-server-protocol/specification#completionItem_resolve
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the completion item to resolve.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a resolved completion item.</returns>
        public Task<LSP.CompletionItem> ResolveCompletionItemAsync(Solution solution, LSP.CompletionItem request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, solution, request, clientCapabilities, cancellationToken);
    }
}
