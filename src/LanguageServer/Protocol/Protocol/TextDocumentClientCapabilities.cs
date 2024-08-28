// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents text document capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class TextDocumentClientCapabilities
    {
        /// <summary>
        /// Gets or sets the synchronization setting.
        /// </summary>
        [JsonPropertyName("synchronization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SynchronizationSetting? Synchronization
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion setting.
        /// </summary>
        [JsonPropertyName("completion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionSetting? Completion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if hover can be dynamically registered.
        /// </summary>
        [JsonPropertyName("hover")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HoverSetting? Hover
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if signature help can be dynamically registered.
        /// </summary>
        [JsonPropertyName("signatureHelp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpSetting? SignatureHelp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if definition can be dynamically registered.
        /// </summary>
        [JsonPropertyName("definition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Definition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if type definition can be dynamically registered.
        /// </summary>
        [JsonPropertyName("typeDefinition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? TypeDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if implementation can be dynamically registered.
        /// </summary>
        [JsonPropertyName("implementation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Implementation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if references can be dynamically registered.
        /// </summary>
        [JsonPropertyName("references")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? References
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document highlight can be dynamically registered.
        /// </summary>
        [JsonPropertyName("documentHighlight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DocumentHighlight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document symbol can be dynamically registered.
        /// </summary>
        [JsonPropertyName("documentSymbol")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentSymbolSetting? DocumentSymbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code action can be dynamically registered.
        /// </summary>
        [JsonPropertyName("codeAction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionSetting? CodeAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code lens can be dynamically registered.
        /// </summary>
        [JsonPropertyName("codeLens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? CodeLens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document link can be dynamically registered.
        /// </summary>
        [JsonPropertyName("documentLink")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DocumentLink
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if colorProvider can be dynamically registered.
        /// </summary>
        [JsonPropertyName("colorProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? ColorProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if formatting can be dynamically registered.
        /// </summary>
        [JsonPropertyName("formatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Formatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if range formatting can be dynamically registered.
        /// </summary>
        [JsonPropertyName("rangeFormatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? RangeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if on type formatting can be dynamically registered.
        /// </summary>
        [JsonPropertyName("onTypeFormatting")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? OnTypeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if rename can be dynamically registered.
        /// </summary>
        [JsonPropertyName("rename")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RenameClientCapabilities? Rename
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting publish diagnostics setting.
        /// </summary>
        [JsonPropertyName("publishDiagnostics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PublishDiagnosticsSetting? PublishDiagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines how folding range is supported.
        /// </summary>
        [JsonPropertyName("foldingRange")]
        public FoldingRangeSetting? FoldingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if linked editing range can be dynamically registered.
        /// </summary>
        [JsonPropertyName("linkedEditingRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting LinkedEditingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a setting indicating whether semantic tokens is supported.
        /// </summary>
        [JsonPropertyName("semanticTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensSetting? SemanticTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [JsonPropertyName("diagnostic")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticSetting? Diagnostic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [JsonPropertyName("inlayHint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintSetting? InlayHint
        {
            get;
            set;
        }
    }
}
