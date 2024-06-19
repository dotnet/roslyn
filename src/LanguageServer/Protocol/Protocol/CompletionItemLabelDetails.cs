// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Additional details for a completion item label.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItemLabelDetails">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    internal class CompletionItemLabelDetails
    {
        /// <summary>
        /// An optional string which is rendered less prominently directly after
        /// <see cref="CompletionItem.Label"/>, without any spacing. Should be
        /// used for function signatures or type annotations.
        /// </summary>
        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional string which is rendered less prominently after
        /// <see cref="CompletionItem.Detail"/>. Should be used for fully qualified
        /// names or file path.
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
