// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a single Capabilities vertex for serialization. See https://github.com/microsoft/lsif-node/blob/main/protocol/src/protocol.ts#L973 for further details.
    /// </summary>
    internal sealed class Capabilities : Vertex
    {
        [JsonProperty("hoverProvider")]
        public bool HoverProvider { get; }

        [JsonProperty("declarationProvider")]
        public bool DeclarationProvider { get; }

        [JsonProperty("definitionProvider")]
        public bool DefinitionProvider { get; }

        [JsonProperty("referencesProvider")]
        public bool ReferencesProvider { get; }

        [JsonProperty("typeDefinitionProvider")]
        public bool TypeDefinitionProvider { get; }

        [JsonProperty("documentSymbolProvider")]
        public bool DocumentSymbolProvider { get; }

        [JsonProperty("foldingRangeProvider")]
        public bool FoldingRangeProvider { get; }

        [JsonProperty("diagnosticProvider")]
        public bool DiagnosticProvider { get; }

        [JsonProperty("semanticTokensProvider")]
        public SemanticTokensCapabilities SemanticTokensProvider { get; }

        public Capabilities(
            IdFactory idFactory,
            bool hoverProvider,
            bool declarationProvider,
            bool definitionProvider,
            bool referencesProvider,
            bool typeDefinitionProvider,
            bool documentSymbolProvider,
            bool foldingRangeProvider,
            bool diagnosticProvider,
            SemanticTokensCapabilities semanticTokensProvider)
            : base(label: "capabilities", idFactory)
        {
            HoverProvider = hoverProvider;
            DeclarationProvider = declarationProvider;
            DefinitionProvider = definitionProvider;
            ReferencesProvider = referencesProvider;
            TypeDefinitionProvider = typeDefinitionProvider;
            DocumentSymbolProvider = documentSymbolProvider;
            FoldingRangeProvider = foldingRangeProvider;
            DiagnosticProvider = diagnosticProvider;
            SemanticTokensProvider = semanticTokensProvider;
        }
    }
}
