// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a semantic tokens vertex for serialization. See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokens for further details.
    /// </summary>
    internal sealed class SemanticTokensResult : Vertex
    {
        [JsonProperty("result")]
        public SemanticTokens Result { get; }

        public SemanticTokensResult(SemanticTokens result, IdFactory idFactory)
            : base(label: "semanticTokensResult", idFactory)
        {
            Result = result;
        }
    }
}
