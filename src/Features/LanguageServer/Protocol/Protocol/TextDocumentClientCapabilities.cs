// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents text document capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class TextDocumentClientCapabilities
    {
        /// <summary>
        /// Gets or sets the synchronization setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("synchronization")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SynchronizationSetting? Synchronization
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completion")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CompletionSetting? Completion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if hover can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("hover")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public HoverSetting? Hover
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if signature help can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("signatureHelp")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpSetting? SignatureHelp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if definition can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("definition")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Definition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if type definition can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("typeDefinition")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? TypeDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if implementation can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("implementation")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Implementation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if references can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("references")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? References
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document highlight can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentHighlight")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DocumentHighlight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document symbol can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentSymbol")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DocumentSymbolSetting? DocumentSymbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code action can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("codeAction")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionSetting? CodeAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code lens can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("codeLens")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? CodeLens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document link can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentLink")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DocumentLink
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if formatting can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("formatting")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? Formatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if range formatting can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rangeFormatting")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? RangeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if on type formatting can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("onTypeFormatting")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? OnTypeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if rename can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rename")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public RenameClientCapabilities? Rename
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting publish diagnostics setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("publishDiagnostics")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public PublishDiagnosticsSetting? PublishDiagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines how folding range is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("foldingRange")]
        public FoldingRangeSetting? FoldingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if linked editing range can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("linkedEditingRange")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting LinkedEditingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a setting indicating whether semantic tokens is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("semanticTokens")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensSetting? SemanticTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("diagnostic")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticSetting? Diagnostic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("inlayHint")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintSetting? InlayHint
        {
            get;
            set;
        }
    }
}
