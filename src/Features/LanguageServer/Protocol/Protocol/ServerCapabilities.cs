// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents server capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#serverCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ServerCapabilities
    {
        /// <summary>
        /// Gets or sets the value which indicates how text document are synced.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("textDocumentSync")]
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
        /// Gets or sets the value which indicates if completions are supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completionProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CompletionOptions? CompletionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides hover support.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("hoverProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, HoverOptions>? HoverProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if signature help is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("signatureHelpProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpOptions? SignatureHelpProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to definition is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("definitionProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DefinitionOptions>? DefinitionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to type definition is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("typeDefinitionProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, TypeDefinitionOptions>? TypeDefinitionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to implementation is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("implementationProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, ImplementationOptions>? ImplementationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether find all references is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("referencesProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, ReferenceOptions>? ReferencesProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports document highlight.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentHighlightProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentHighlightOptions>? DocumentHighlightProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document symbols are supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentSymbolProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentSymbolOptions>? DocumentSymbolProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether code actions are supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("codeActionProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, CodeActionOptions>? CodeActionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if code lens is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("codeLensProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CodeLensOptions? CodeLensProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document link is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentLinkProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DocumentLinkOptions? DocumentLinkProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document color is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("colorProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentColorOptions>? DocumentColorProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document formatting is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentFormattingProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentFormattingOptions>? DocumentFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document range formatting is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentRangeFormattingProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, DocumentRangeFormattingOptions>? DocumentRangeFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document on type formatting is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentOnTypeFormattingProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether rename is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("renameProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, RenameOptions>? RenameProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if folding range is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("foldingRangeProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, FoldingRangeOptions>? FoldingRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if execute command is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("executeCommandProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public ExecuteCommandOptions? ExecuteCommandProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether workspace symbols are supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("workspaceSymbolProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, WorkspaceSymbolOptions>? WorkspaceSymbolProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets experimental server capabilities.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("experimental")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object? Experimental
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports linked editing range.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("linkedEditingRangeProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, LinkedEditingRangeOptions>? LinkedEditingRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if semantic tokens is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("semanticTokensProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensOptions? SemanticTokensOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for pull diagnostics.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("diagnosticProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticOptions? DiagnosticOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for inlay hints.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("inlayHintProvider")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, InlayHintOptions>? InlayHintOptions
        {
            get;
            set;
        }
    }
}
