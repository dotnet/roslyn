// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension class for ClientCapabilities with fields specific to Visual Studio.
    /// </summary>
    internal class VSInternalClientCapabilities : ClientCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether client supports Visual Studio extensions.
        /// </summary>
        [JsonPropertyName("_vs_supportsVisualStudioExtensions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SupportsVisualStudioExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating what level of snippet support is available from Visual Studio Client.
        /// v1.0 refers to only default tab stop support i.e. support for $0 which manipualtes the cursor position.
        /// </summary>
        [JsonPropertyName("_vs_supportedSnippetVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalSnippetSupportLevel? SupportedSnippetVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether client supports omitting document text in textDocument/didOpen notifications.
        /// </summary>
        [JsonPropertyName("_vs_supportsNotIncludingTextInTextDocumentDidOpen")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SupportsNotIncludingTextInTextDocumentDidOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports string based response kinds
        /// instead of enum based response kinds.
        /// </summary>
        [JsonPropertyName("_vs_supportsIconExtensions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SupportsIconExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client provides support for diagnostic pull requests.
        /// </summary>
        [JsonPropertyName("_vs_supportsDiagnosticRequests")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SupportsDiagnosticRequests
        {
            get;
            set;
        }
    }
}