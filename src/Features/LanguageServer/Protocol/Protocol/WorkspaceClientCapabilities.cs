// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents workspace capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class WorkspaceClientCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether apply edit is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("applyEdit")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ApplyEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace edit setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("workspaceEdit")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public WorkspaceEditSetting? WorkspaceEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if did change configuration can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("didChangeConfiguration")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DidChangeConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if did change watched files can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("didChangeWatchedFiles")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? DidChangeWatchedFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if symbols can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("symbol")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SymbolSetting? Symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if execute command can be dynamically registered.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("executeCommand")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DynamicRegistrationSetting? ExecuteCommand
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets capabilities specific to the semantic token requests scoped to the workspace.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("semanticTokens")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SemanticTokensWorkspaceSetting? SemanticTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets capabilities indicating what support the client has for workspace pull diagnostics.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("diagnostics")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticWorkspaceSetting? Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the capabilities if client support 'workspace/configuration' requests.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("configuration")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets of sets capabilities specific to the inlay hint requests scoped to the workspace.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("inlayHint")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintWorkspaceSetting? InlayHint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets of sets capabilities specific to the code lens requests scoped to the workspace.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("codeLens")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CodeLensWorkspaceSetting? CodeLens
        {
            get;
            set;
        }
    }
}
