// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension class for ServerCapabilities with fields specific to Visual Studio.
    /// </summary>
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
        [JsonPropertyName("_vs_disableGoToWorkspaceSymbols")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DisableGoToWorkspaceSymbols
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether document/_ms_references is supported.
        /// </summary>
        [JsonPropertyName("_vs_ReferencesProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MSReferencesProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports OnAutoInsert.
        /// </summary>
        [JsonPropertyName("_vs_onAutoInsertProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalDocumentOnAutoInsertOptions? OnAutoInsertProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server requires document text to be included in textDocument/didOpen notifications.
        /// </summary>
        /// <remarks>This capability is not intended to be included into the official LSP, hence _ms_ prefix.</remarks>
        [JsonPropertyName("_vs_doNotIncludeTextInTextDocumentDidOpen")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DoNotIncludeTextInTextDocumentDidOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support to resolve string based response kinds.
        /// </summary>
        [JsonPropertyName("_vs_KindDescriptionResolveProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool KindDescriptionResolveProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for diagnostic pull requests.
        /// </summary>
        [JsonPropertyName("_vs_supportsDiagnosticRequests")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SupportsDiagnosticRequests
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets server specified options for diagnostic pull requests.
        /// </summary>
        [JsonPropertyName("_vs_diagnosticProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalDiagnosticOptions? DiagnosticProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for inline completion requests.
        /// </summary>
        [JsonPropertyName("_vs_inlineCompletionOptions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalInlineCompletionOptions? InlineCompletionOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for spell checking.
        /// </summary>
        [JsonPropertyName("_vs_spellCheckingProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SpellCheckingProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports validating breakable ranges.
        /// </summary>
        [JsonPropertyName("_vs_breakableRangeProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool BreakableRangeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports uri presentation.
        /// </summary>
        [JsonPropertyName("_vs_uriPresentationProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool UriPresentationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server supports text presentation.
        /// </summary>
        [JsonPropertyName("_vs_textPresentationProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TextPresentationProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates what support the server has for code mapping.
        /// </summary>
        [JsonPropertyName("_vs_mapCodeProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MapCodeProvider
        {
            get;
            set;
        }
    }
}
