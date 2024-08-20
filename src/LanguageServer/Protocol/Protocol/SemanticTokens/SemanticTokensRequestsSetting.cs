// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents which semantic token requests are supported by the client.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    internal class SemanticTokensRequestsSetting
    {
        /// <summary>
        /// The client will send the <c>textDocument/semanticTokens/range</c> request
        /// if the server provides a corresponding handler.
        /// </summary>
        [JsonPropertyName("range")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, object>? Range { get; set; }

        /// <summary>
        /// The client will send the <c>textDocument/semanticTokens/full</c> request
        /// if the server provides a corresponding handler.
        /// </summary>
        [JsonPropertyName("full")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, SemanticTokensRequestsFullSetting>? Full { get; set; }
    }
}
