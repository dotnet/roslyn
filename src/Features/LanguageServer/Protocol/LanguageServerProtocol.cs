// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Implements Language Server Protocol
    /// </summary>
    public sealed class LanguageServerProtocol
    {
        // for now, it is null but later we might use this to provide info on types on dlls
        private readonly IMetadataAsSourceFileService _metadataAsSourceService = null;

        // TODO - Move hierarchicalDocumentSymbolSupport to client capabilities.
        // https://github.com/dotnet/roslyn/projects/45#card-20033973
        private readonly bool _hierarchicalDocumentSymbolSupport;
        private readonly LSP.ClientCapabilities _clientCapabilities;

        public LanguageServerProtocol(LSP.ClientCapabilities clientCapabilities, bool hierarchicalDocumentSymbolSupport)
        {
            _hierarchicalDocumentSymbolSupport = hierarchicalDocumentSymbolSupport;
            _clientCapabilities = clientCapabilities;
        }

        /// <summary>
        /// Returns the capabilities of this LSP server.
        /// </summary>
        public LSP.ServerCapabilities GetServerCapabilities()
        {
            return new LSP.ServerCapabilities
            {
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentHighlightProvider = true,
                DocumentSymbolProvider = true,
                WorkspaceSymbolProvider = true,
                HoverProvider = true,
            };
        }

        /// <summary>
        /// Answers a document symbols request by returning a list of symbols in the document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentSymbol
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the document to get symbols from.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the document.</returns>
        public async Task<object[]> GetDocumentSymbolsAsync(Solution solution, LSP.DocumentSymbolParams request, CancellationToken cancellationToken)
        {
            return await DocumentSymbolsHandler.GetDocumentSymbolsAsync(solution, request, _hierarchicalDocumentSymbolSupport, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a workspace symbols request by providing a list of symbols found in a given workspace.
        /// https://microsoft.github.io/language-server-protocol/specification#workspace_symbol
        /// </summary>
        /// <param name="solution">the current solution.</param>
        /// <param name="request">the workspace request with the query to invoke.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the workspace.</returns>
        public async Task<LSP.SymbolInformation[]> GetWorkspaceSymbolsAsync(Solution solution, LSP.WorkspaceSymbolParams request, CancellationToken cancellationToken)
        {
            return await WorkspaceSymbolsHandler.GetWorkspaceSymbolsAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a Hover request by returning the quick info at the requested location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
        /// </summary>
        /// <param name="solution">the solution containing any documents in the request.</param>
        /// <param name="request">the hover requesst.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the Hover using MarkupContent.</returns>
        public async Task<LSP.Hover> GetHoverAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return await HoverHandler.GetHoverAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a goto definition request by returning the location for a given symbol definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_definition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the symbol to go to.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location of a given symbol.</returns>
        public async Task<LSP.Location[]> GoToDefinitionAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return await GoToDefinitionHandler.GetDefinitionAsync(solution, request, false, _metadataAsSourceService, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a goto type definition request by returning the location of a given type definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_typeDefinition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the type to go to.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location of a type definition.</returns>
        public async Task<LSP.Location[]> GoToTypeDefinitionAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return await GoToDefinitionHandler.GetDefinitionAsync(solution, request, true, _metadataAsSourceService, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a find references request by returning the location of references to the given symbol.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_references
        /// </summary>
        /// <param name="solution">the solution containing the symbol.</param>
        /// <param name="request">the request symbol document location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of locations of references to the given symbol.</returns>
        public async Task<LSP.Location[]> FindAllReferencesAsync(Solution solution, LSP.ReferenceParams request, CancellationToken cancellationToken)
        {
            return await FindAllReferencesHandler.FindAllReferencesAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a goto implementation request by returning the implementation location(s) of a given symbol.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_implementation
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document symbol location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location(s) of the implementations of the symbol.</returns>
        public async Task<LSP.Location[]> GotoImplementationAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return await GoToImplementationHandler.GotoImplementationAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a document highlights request by returning the highlights for a given document location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentHighlight
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the highlights in the document for the given document location.</returns>
        public async Task<LSP.DocumentHighlight[]> GetDocumentHighlightAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return await DocumentHighlightsHandler.GetDocumentHighlightsAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Answers a folding range request by returning all folding ranges in a given document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_foldingRange
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of folding ranges in the document.</returns>
        public async Task<LSP.FoldingRange[]> GetFoldingRangeAsync(Solution solution, LSP.FoldingRangeParams request, CancellationToken cancellationToken)
        {
            return await FoldingRangesHandler.GetFoldingRangeAsync(solution, request, cancellationToken).ConfigureAwait(false);
        }
    }
}
