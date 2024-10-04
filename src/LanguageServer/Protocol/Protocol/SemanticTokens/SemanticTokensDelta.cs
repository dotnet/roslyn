// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a response from a semantic tokens Document provider Edits request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensDelta">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    internal class SemanticTokensDelta
    {
        /// <summary>
        /// The ID of this result, which can then be used as a base for future deltas.
        /// </summary>
        [JsonPropertyName("resultId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResultId { get; set; }

        /// <summary>
        /// Gets or sets an array of edits to apply to a previous response from a
        /// semantic tokens Document provider.
        /// </summary>
        [JsonPropertyName("edits")]
        [JsonRequired]
        public SemanticTokensEdit[] Edits { get; set; }
    }
}
