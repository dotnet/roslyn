﻿// Licensed to the .NET Foundation under one or more agreements.
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
        [DataMember(Name = "applyEdit")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ApplyEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace edit setting.
        /// </summary>
        [DataMember(Name = "workspaceEdit")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceEditSetting? WorkspaceEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if did change configuration can be dynamically registered.
        /// </summary>
        [DataMember(Name = "didChangeConfiguration")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? DidChangeConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if did change watched files can be dynamically registered.
        /// </summary>
        [DataMember(Name = "didChangeWatchedFiles")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? DidChangeWatchedFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if symbols can be dynamically registered.
        /// </summary>
        [DataMember(Name = "symbol")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SymbolSetting? Symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the setting which determines if execute command can be dynamically registered.
        /// </summary>
        [DataMember(Name = "executeCommand")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? ExecuteCommand
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets capabilities specific to the semantic token requests scoped to the workspace.
        /// </summary>
        [DataMember(Name = "semanticTokens")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SemanticTokensWorkspaceSetting? SemanticTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets capabilities indicating what support the client has for workspace pull diagnostics.
        /// </summary>
        [DataMember(Name = "diagnostics")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticWorkspaceSetting? Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the capabilities if client support 'workspace/configuration' requests.
        /// </summary>
        [DataMember(Name = "configuration")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets of sets capabilities specific to the inlay hint requests scoped to the workspace.
        /// </summary>
        [DataMember(Name = "inlayHint")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InlayHintWorkspaceSetting? InlayHint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets of sets capabilities specific to the code lens requests scoped to the workspace.
        /// </summary>
        [DataMember(Name = "codeLens")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeLensWorkspaceSetting? CodeLens
        {
            get;
            set;
        }
    }
}
