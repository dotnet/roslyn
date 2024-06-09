// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents text document capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class TextDocumentClientCapabilities
    {
        // NOTE: these are kept in the same order as the spec to make them easier to update

        /// <summary>
        /// Gets or sets the synchronization setting.
        /// </summary>
        [JsonPropertyName("synchronization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SynchronizationSetting? Synchronization { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/completion` request.
        /// </summary>
        [JsonPropertyName("completion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionSetting? Completion { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/hover` request
        /// </summary>
        [JsonPropertyName("hover")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HoverSetting? Hover { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/signatureHelp` request
        /// </summary>
        [JsonPropertyName("signatureHelp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpSetting? SignatureHelp { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/declaration` request
        /// </summary>
        /// <remarks>Since LSP 3.14</remarks>
        [JsonPropertyName("declaration")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeclarationClientCapabilities? Declaration { get; init; }

        /// <summary>
        /// Capabilities specific to the `textDocument/definition` request
        /// </summary>
        [JsonPropertyName("definition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DefinitionClientCapabilities? Definition { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/typeDefinition` request.
        /// </summary>
        /// <remarks>Since LSP 3.6</remarks>
        [JsonPropertyName("typeDefinition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TypeDefinitionClientCapabilities? TypeDefinition { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/implementation` request.
        /// </summary>
        /// <remarks>Since LSP 3.6</remarks>
        [JsonPropertyName("implementation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImplementationClientCapabilities? Implementation { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/references` request.
        /// </summary>
        [JsonPropertyName("references")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReferenceClientCapabilities? References { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/documentHighlight` request.
        /// </summary>
        [JsonPropertyName("documentHighlight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentHighlightClientCapabilities? DocumentHighlight { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/documentSymbol` request.
        /// </summary>
        [JsonPropertyName("documentSymbol")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentSymbolSetting? DocumentSymbol { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/codeAction` request.
        /// </summary>
        [JsonPropertyName("codeAction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionSetting? CodeAction { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/codeLens` request.
        /// </summary>
        [JsonPropertyName("codeLens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeLensClientCapabilities? CodeLens { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/documentLink` request.
        /// </summary>
        [JsonPropertyName("documentLink")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentLinkClientCapabilities? DocumentLink { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/documentColor` and the `textDocument/colorPresentation` request.
        /// </summary>
        /// <remarks>Since LSP 3.6</remarks>
        [JsonPropertyName("colorProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentColorClientCapabilities? ColorProvider { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/formatting` request.
        /// </summary>
        [JsonPropertyName("formatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentFormattingClientCapabilities? Formatting { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/rangeFormatting` request.
        /// </summary>
        [JsonPropertyName("rangeFormatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RangeFormattingClientCapabilities? RangeFormatting { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/onTypeFormatting` request.
        /// </summary>
        [JsonPropertyName("onTypeFormatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OnTypeFormattingClientCapabilities? OnTypeFormatting { get; set; }

        /// <summary>
        /// Gets or sets the setting which determines if rename can be dynamically registered.
        /// </summary>
        [JsonPropertyName("rename")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RenameClientCapabilities? Rename { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/publishDiagnostics` notification.
        /// </summary>
        [JsonPropertyName("publishDiagnostics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PublishDiagnosticsSetting? PublishDiagnostics { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/foldingRange` request.
        /// </summary>
        /// <remarks>Since LSP 3.10</remarks>
        [JsonPropertyName("foldingRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FoldingRangeSetting? FoldingRange { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/selectionRange` request.
        /// </summary>
        /// <remarks>Since LSP 3.15</remarks>
        [JsonPropertyName("selectionRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SelectionRangeClientCapabilities? SelectionRange { get; set; }

        /// <summary>
        /// Gets or sets the setting which determines if linked editing range can be dynamically registered.
        /// </summary>
        [JsonPropertyName("linkedEditingRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting LinkedEditingRange { get; set; }

        /// <summary>
        /// Capabilities specific to the various call hierarchy requests.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("callHierarchy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CallHierarchyClientCapabilities CallHierarchy { get; init; }

        /// <summary>
        /// Capabilities specific to the various semantic token requests.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("semanticTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensSetting? SemanticTokens { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/moniker` request.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("moniker")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MonikerClientCapabilities? Moniker { get; set; }

        /// <summary>
        /// Capabilities specific to the various type hierarchy requests.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("typeHierarchy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TypeHierarchyClientCapabilities? TypeHierarchy { get; init; }

        /// <summary>
        /// Capabilities specific to the `textDocument/inlineValue` request.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("inlineValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlineValueClientCapability? InlineValue { get; set; }

        /// <summary>
        /// Capabilities specific to the `textDocument/inlayHint` request.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("inlayHint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintSetting? InlayHint { get; set; }

        /// <summary>
        /// Capabilities specific to the diagnostic pull model.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("diagnostic")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticSetting? Diagnostic { get; set; }
    }
}
