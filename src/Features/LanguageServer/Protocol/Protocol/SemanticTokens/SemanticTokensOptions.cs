// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Initialization options for semantic tokens support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SemanticTokensOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets a legend describing how semantic token types and modifiers are encoded in responses.
        /// </summary>
        [JsonPropertyName("legend")]
        public SemanticTokensLegend Legend { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether semantic tokens Range provider requests are supported.
        /// </summary>
        [JsonPropertyName("range")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, object>? Range { get; set; }

        /// <summary>
        /// Gets or sets whether or not the server supports providing semantic tokens for a full document.
        /// </summary>
        [JsonPropertyName("full")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<bool, SemanticTokensFullOptions>? Full { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [JsonPropertyName("workDoneProgress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool WorkDoneProgress { get; init; }
    }
}
