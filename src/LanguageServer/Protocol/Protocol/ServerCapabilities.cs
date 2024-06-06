// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents server capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#serverCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class ServerCapabilities
    {
        // NOTE: these are kept in the same order as the spec to make them easier to update

        /// <summary>
        /// The position encoding the server picked from the encodings offered
        /// by the client via the client capability `general.positionEncodings`.
        ///<para>
        /// If the client didn't provide any position encodings the only valid
        /// value that a server can return is 'utf-16'.
        /// If omitted it defaults to 'utf-16'.
        ///</para>
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("positionEncoding")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PositionEncodingKind? PositionEncoding { get; init; }

        /// <summary>
        /// Gets or sets the value which indicates how text document are synced.
        /// </summary>
        [JsonPropertyName("textDocumentSync")]
        [JsonConverter(typeof(TextDocumentSyncConverter))]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
        public TextDocumentSyncOptions? TextDocumentSync
        {
            get;
            set;
        } = new TextDocumentSyncOptions
        {
            OpenClose = true,
            Change = TextDocumentSyncKind.None,
            Save = new SaveOptions
            {
                IncludeText = false,
            },
        };

        /// <summary>
        /// Defines how notebook documents are synced.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("notebookDocumentSync")]
        public SumType<NotebookDocumentSyncOptions, NotebookDocumentSyncRegistrationOptions>? NotebookDocumentSync { get; init; }

        /// <summary>
        /// Gets or sets the value which indicates if completions are supported.
        /// </summary>
        [JsonPropertyName("completionProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionOptions? CompletionProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides hover support.
        /// </summary>
        [JsonPropertyName("hoverProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, HoverOptions>? HoverProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if signature help is supported.
        /// </summary>
        [JsonPropertyName("signatureHelpProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpOptions? SignatureHelpProvider { get; set; }

        /// <summary>
        /// The server provides Go to Declaration support.
        /// </summary>
        /// <remarks>Since LSP 3.14</remarks>
        [JsonPropertyName("declarationProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DeclarationOptions, DeclarationRegistrationOptions>? DeclarationProvider { get; init; }

        /// <summary>
        /// The server provides Go to Definition support.
        /// </summary>
        [JsonPropertyName("definitionProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DefinitionOptions>? DefinitionProvider { get; set; }

        /// <summary>
        /// The server provides Go to Type Definition support.
        /// </summary>
        /// <remarks>Since LSP 3.6</remarks>
        [JsonPropertyName("typeDefinitionProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, TypeDefinitionOptions, TypeDefinitionRegistrationOptions>? TypeDefinitionProvider { get; set; }

        /// <summary>
        /// The server provides Go to Implementation support.
        /// </summary>
        /// <remarks>Since LSP 3.6</remarks>
        [JsonPropertyName("implementationProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, ImplementationOptions, ImplementationRegistrationOptions>? ImplementationProvider { get; set; }

        /// <summary>
        /// The server provides Find References support.
        /// </summary>
        [JsonPropertyName("referencesProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, ReferenceOptions>? ReferencesProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports document highlight.
        /// </summary>
        [JsonPropertyName("documentHighlightProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentHighlightOptions>? DocumentHighlightProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether document symbols are supported.
        /// </summary>
        [JsonPropertyName("documentSymbolProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentSymbolOptions>? DocumentSymbolProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether code actions are supported.
        /// </summary>
        [JsonPropertyName("codeActionProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, CodeActionOptions>? CodeActionProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if code lens is supported.
        /// </summary>
        [JsonPropertyName("codeLensProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeLensOptions? CodeLensProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if document link is supported.
        /// </summary>
        [JsonPropertyName("documentLinkProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentLinkOptions? DocumentLinkProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if document color is supported.
        /// </summary>
        [JsonPropertyName("colorProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentColorOptions>? DocumentColorProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether document formatting is supported.
        /// </summary>
        [JsonPropertyName("documentFormattingProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentFormattingOptions>? DocumentFormattingProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether document range formatting is supported.
        /// </summary>
        [JsonPropertyName("documentRangeFormattingProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentRangeFormattingOptions>? DocumentRangeFormattingProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if document on type formatting is supported.
        /// </summary>
        [JsonPropertyName("documentOnTypeFormattingProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether rename is supported.
        /// </summary>
        [JsonPropertyName("renameProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, RenameOptions>? RenameProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if folding range is supported.
        /// </summary>
        [JsonPropertyName("foldingRangeProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, FoldingRangeOptions>? FoldingRangeProvider { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates if execute command is supported.
        /// </summary>
        [JsonPropertyName("executeCommandProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ExecuteCommandOptions? ExecuteCommandProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports linked editing range.
        /// </summary>
        [JsonPropertyName("linkedEditingRangeProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, LinkedEditingRangeOptions>? LinkedEditingRangeProvider { get; set; }

        /// <summary>
        /// The server provides call hierarchy support.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("callHierarchyProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, CallHierarchyOptions, CallHierarchyRegistrationOptions>? CallHierarchyProvider { get; init; }

        /// <summary>
        /// Gets or sets the value which indicates if semantic tokens is supported.
        /// </summary>
        [JsonPropertyName("semanticTokensProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensOptions? SemanticTokensOptions { get; set; }

        /// <summary>
        /// The server provides type hierarchy support.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("typeHierarchyProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, TypeHierarchyOptions, TypeHierarchyRegistrationOptions>? TypeHierarchyProvider { get; init; }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for inlay hints.
        /// </summary>
        [JsonPropertyName("inlayHintProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, InlayHintOptions>? InlayHintOptions { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for pull diagnostics.
        /// </summary>
        [JsonPropertyName("diagnosticProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticOptions? DiagnosticOptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether workspace symbols are supported.
        /// </summary>
        [JsonPropertyName("workspaceSymbolProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, WorkspaceSymbolOptions>? WorkspaceSymbolProvider { get; set; }

        /// <summary>
        /// Gets or sets experimental server capabilities.
        /// </summary>
        [JsonPropertyName("experimental")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Experimental { get; set; }
    }
}
