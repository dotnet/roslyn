// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// Extension class for ServerCapabilities with fields specific to Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSInternalServerCapabilities : VSServerCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not GoTo's integration with
        /// 'workspace/symbol' and the deprecated 16.3 'workspace/beginSymbol' messages
        /// should be disabled.
        /// </summary>
        /// <remarks>
        /// This is provided to facilitate transition from in-proc to OOP for teams that
        /// currently own both a Language Server for Ctrl+Q and a GoTo provider.
        /// </remarks>
        [DataMember(Name = "_vs_disableGoToWorkspaceSymbols")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DisableGoToWorkspaceSymbols
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document/_ms_references is supported.
        /// </summary>
        [DataMember(Name = "_vs_ReferencesProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MSReferencesProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports OnAutoInsert.
        /// </summary>
        [DataMember(Name = "_vs_onAutoInsertProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalDocumentOnAutoInsertOptions? OnAutoInsertProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server requires document text to be included in textDocument/didOpen notifications.
        /// </summary>
        /// <remarks>This capability is not intended to be included into the official LSP, hence _ms_ prefix.</remarks>
        [DataMember(Name = "_vs_doNotIncludeTextInTextDocumentDidOpen")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DoNotIncludeTextInTextDocumentDidOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support to resolve string based response kinds.
        /// </summary>
        [DataMember(Name = "_vs_KindDescriptionResolveProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool KindDescriptionResolveProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for diagnostic pull requests.
        /// </summary>
        [DataMember(Name = "_vs_supportsDiagnosticRequests")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsDiagnosticRequests
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets server specified options for diagnostic pull requests.
        /// </summary>
        [DataMember(Name = "_vs_diagnosticProvider")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalDiagnosticOptions? DiagnosticProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for inline completion requests.
        /// </summary>
        [DataMember(Name = "_vs_inlineCompletionOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalInlineCompletionOptions? InlineCompletionOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for spell checking.
        /// </summary>
        [DataMember(Name = "_vs_spellCheckingProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SpellCheckingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports validating breakable ranges.
        /// </summary>
        [DataMember(Name = "_vs_breakableRangeProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool BreakableRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports uri presentation.
        /// </summary>
        [DataMember(Name = "_vs_uriPresentationProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool UriPresentationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports text presentation.
        /// </summary>
        [DataMember(Name = "_vs_textPresentationProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool TextPresentationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for code mapping.
        /// </summary>
        [DataMember(Name = "_vs_mapCodeProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MapCodeProvider
        {
            get;
            set;
        }
    }
}
