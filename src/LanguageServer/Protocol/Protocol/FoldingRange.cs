// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a folding range in a document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRange">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class FoldingRange
    {
        /// <summary>
        /// Gets or sets the start line value.
        /// </summary>
        [JsonPropertyName("startLine")]
        public int StartLine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the start character value.
        /// </summary>
        [JsonPropertyName("startCharacter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StartCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the end line value.
        /// </summary>
        [JsonPropertyName("endLine")]
        public int EndLine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the end character value.
        /// </summary>
        [JsonPropertyName("endCharacter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? EndCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the folding range kind.
        /// </summary>
        [JsonPropertyName("kind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FoldingRangeKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collapsedText.
        /// </summary>
        [JsonPropertyName("collapsedText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CollapsedText
        {
            get;
            set;
        }
    }
}
