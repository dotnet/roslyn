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
        [DataMember(Name = "textDocumentSync")]
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
        [DataMember(Name = "completionProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionOptions? CompletionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides hover support.
        /// </summary>
        [DataMember(Name = "hoverProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, HoverOptions>? HoverProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if signature help is supported.
        /// </summary>
        [DataMember(Name = "signatureHelpProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SignatureHelpOptions? SignatureHelpProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to definition is supported.
        /// </summary>
        [DataMember(Name = "definitionProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DefinitionOptions>? DefinitionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to type definition is supported.
        /// </summary>
        [DataMember(Name = "typeDefinitionProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, TypeDefinitionOptions>? TypeDefinitionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether go to implementation is supported.
        /// </summary>
        [DataMember(Name = "implementationProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, ImplementationOptions>? ImplementationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether find all references is supported.
        /// </summary>
        [DataMember(Name = "referencesProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, ReferenceOptions>? ReferencesProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports document highlight.
        /// </summary>
        [DataMember(Name = "documentHighlightProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DocumentHighlightOptions>? DocumentHighlightProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document symbols are supported.
        /// </summary>
        [DataMember(Name = "documentSymbolProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DocumentSymbolOptions>? DocumentSymbolProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether code actions are supported.
        /// </summary>
        [DataMember(Name = "codeActionProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, CodeActionOptions>? CodeActionProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if code lens is supported.
        /// </summary>
        [DataMember(Name = "codeLensProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeLensOptions? CodeLensProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document link is supported.
        /// </summary>
        [DataMember(Name = "documentLinkProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DocumentLinkOptions? DocumentLinkProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document color is supported.
        /// </summary>
        [DataMember(Name = "colorProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DocumentColorOptions>? DocumentColorProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document formatting is supported.
        /// </summary>
        [DataMember(Name = "documentFormattingProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DocumentFormattingOptions>? DocumentFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document range formatting is supported.
        /// </summary>
        [DataMember(Name = "documentRangeFormattingProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, DocumentRangeFormattingOptions>? DocumentRangeFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if document on type formatting is supported.
        /// </summary>
        [DataMember(Name = "documentOnTypeFormattingProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether rename is supported.
        /// </summary>
        [DataMember(Name = "renameProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, RenameOptions>? RenameProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if folding range is supported.
        /// </summary>
        [DataMember(Name = "foldingRangeProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, FoldingRangeOptions>? FoldingRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if execute command is supported.
        /// </summary>
        [DataMember(Name = "executeCommandProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ExecuteCommandOptions? ExecuteCommandProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether workspace symbols are supported.
        /// </summary>
        [DataMember(Name = "workspaceSymbolProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, WorkspaceSymbolOptions>? WorkspaceSymbolProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets experimental server capabilities.
        /// </summary>
        [DataMember(Name = "experimental")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? Experimental
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports linked editing range.
        /// </summary>
        [DataMember(Name = "linkedEditingRangeProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, LinkedEditingRangeOptions>? LinkedEditingRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates if semantic tokens is supported.
        /// </summary>
        [DataMember(Name = "semanticTokensProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SemanticTokensOptions? SemanticTokensOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for pull diagnostics.
        /// </summary>
        [DataMember(Name = "diagnosticProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticOptions? DiagnosticOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for inlay hints.
        /// </summary>
        [DataMember(Name = "inlayHintProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, InlayHintOptions>? InlayHintOptions
        {
            get;
            set;
        }
    }
}
