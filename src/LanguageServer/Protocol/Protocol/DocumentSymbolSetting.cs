// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client capabilities specific to the <c>textDocument/documentSymbol</c> request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbolClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class DocumentSymbolSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Specific capabilities for <see cref="SymbolKind"/> in <c>textDocument/documentSymbol</c> requests
        /// </summary>
        [JsonPropertyName("symbolKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SymbolKindSetting? SymbolKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the document has hierarchical symbol support.
        /// </summary>
        [JsonPropertyName("hierarchicalDocumentSymbolSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool HierarchicalDocumentSymbolSupport
        {
            get;
            set;
        }

        /// <summary>
        /// The client supports tags on <see cref="SymbolInformation"/>. Tags are supported on
        /// <see cref="DocumentSymbol"/> if <see cref="HierarchicalDocumentSymbolSupport"/> is
        /// set to <see langword="true"/>.
        /// <para>
        /// Clients supporting tags have to handle unknown tags gracefully.
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("tagSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SymbolTagSupport? TagSupport { get; init; }

        /// <summary>
        /// The client supports an additional label presented in the UI when
        /// registering a document symbol provider.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("labelSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool LabelSupport { get; init; }
    }
}
