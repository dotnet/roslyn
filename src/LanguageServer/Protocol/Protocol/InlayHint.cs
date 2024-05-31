// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A class representing inlay hints that appear next to parameters or types.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHint">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InlayHint
    {
        /// <summary>
        /// Gets or sets the position that the inlay hint applies to.
        /// </summary>
        [JsonPropertyName("position")]
        public Position Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the label associated with this inlay hint.
        /// </summary>
        [JsonPropertyName("label")]
        public SumType<string, InlayHintLabelPart[]> Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the InlayHintKind associated with this inlay hint.
        /// </summary>
        [JsonPropertyName("kind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TextEdits associated with this inlay hint.
        /// </summary>
        [JsonPropertyName("textEdits")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextEdit[]? TextEdits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tooltip of this inlay hint.
        /// </summary>
        [JsonPropertyName("tooltip")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<string, MarkupContent>? ToolTip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the padding before this inlay hint.
        /// </summary>
        [JsonPropertyName("paddingLeft")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PaddingLeft
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the padding after this inlay hint.
        /// </summary>
        [JsonPropertyName("paddingRight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PaddingRight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data that should be preserved between a textDocument/inlayHint request and a inlayHint/resolve request.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
