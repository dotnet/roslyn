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
        [DataMember(Name = "synchronization")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SynchronizationSetting? Synchronization
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion setting.
        /// </summary>
        [DataMember(Name = "completion")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionSetting? Completion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if hover can be dynamically registered.
        /// </summary>
        [DataMember(Name = "hover")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public HoverSetting? Hover
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if signature help can be dynamically registered.
        /// </summary>
        [DataMember(Name = "signatureHelp")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SignatureHelpSetting? SignatureHelp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if definition can be dynamically registered.
        /// </summary>
        [DataMember(Name = "definition")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? Definition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if type definition can be dynamically registered.
        /// </summary>
        [DataMember(Name = "typeDefinition")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? TypeDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the settings which determines if implementation can be dynamically registered.
        /// </summary>
        [DataMember(Name = "implementation")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? Implementation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if references can be dynamically registered.
        /// </summary>
        [DataMember(Name = "references")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? References
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document highlight can be dynamically registered.
        /// </summary>
        [DataMember(Name = "documentHighlight")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? DocumentHighlight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document symbol can be dynamically registered.
        /// </summary>
        [DataMember(Name = "documentSymbol")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DocumentSymbolSetting? DocumentSymbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code action can be dynamically registered.
        /// </summary>
        [DataMember(Name = "codeAction")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeActionSetting? CodeAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if code lens can be dynamically registered.
        /// </summary>
        [DataMember(Name = "codeLens")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? CodeLens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if document link can be dynamically registered.
        /// </summary>
        [DataMember(Name = "documentLink")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? DocumentLink
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if formatting can be dynamically registered.
        /// </summary>
        [DataMember(Name = "formatting")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? Formatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if range formatting can be dynamically registered.
        /// </summary>
        [DataMember(Name = "rangeFormatting")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? RangeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if on type formatting can be dynamically registered.
        /// </summary>
        [DataMember(Name = "onTypeFormatting")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? OnTypeFormatting
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if rename can be dynamically registered.
        /// </summary>
        [DataMember(Name = "rename")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RenameClientCapabilities? Rename
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting publish diagnostics setting.
        /// </summary>
        [DataMember(Name = "publishDiagnostics")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public PublishDiagnosticsSetting? PublishDiagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines how folding range is supported.
        /// </summary>
        [DataMember(Name = "foldingRange")]
        public FoldingRangeSetting? FoldingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if linked editing range can be dynamically registered.
        /// </summary>
        [DataMember(Name = "linkedEditingRange")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting LinkedEditingRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a setting indicating whether semantic tokens is supported.
        /// </summary>
        [DataMember(Name = "semanticTokens")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SemanticTokensSetting? SemanticTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [DataMember(Name = "diagnostic")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticSetting? Diagnostic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines what support the client has for pull diagnostics.
        /// </summary>
        [DataMember(Name = "inlayHint")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InlayHintSetting? InlayHint
        {
            get;
            set;
        }
    }
}
