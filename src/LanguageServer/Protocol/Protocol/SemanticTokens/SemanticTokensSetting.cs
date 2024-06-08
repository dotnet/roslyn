// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client settings for semantic tokens.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SemanticTokensSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating which requests the client supports and might send to the server
        /// depending on the server's capability.
        /// </summary>
        [JsonPropertyName("requests")]
        public SemanticTokensRequestsSetting Requests { get; set; }

        /// <summary>
        /// Gets or sets an array of token types supported by the client for encoding
        /// semantic tokens.
        /// </summary>
        [JsonPropertyName("tokenTypes")]
        public string[] TokenTypes { get; set; }

        /// <summary>
        /// Gets or sets an array of token modifiers supported by the client for encoding
        /// semantic tokens.
        /// </summary>
        [JsonPropertyName("tokenModifiers")]
        public string[] TokenModifiers { get; set; }

        /// <summary>
        /// Gets or sets an array of formats the clients supports.
        /// </summary>
        [JsonPropertyName("formats")]
        public SemanticTokenFormat[] Formats { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports tokens that can overlap each other.
        /// </summary>
        [JsonPropertyName("overlappingTokenSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool OverlappingTokenSupport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports tokens that can span multiple lines.
        /// </summary>
        [JsonPropertyName("multilineTokenSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MultilineTokenSupport { get; set; }
    }
}
