// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Additional details for a completion item label.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#completionItemLabelDetails">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionItemLabelDetails
    {
        /// <summary>
        /// Gets or sets an optional string which is rendered less prominently directly after label, without any spacing.
        /// </summary>
        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional string which is rendered less prominently after detail.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description
        {
            get;
            set;
        }
    }
}
