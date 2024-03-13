// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// A class representing inlay hints that appear next to parameters or types.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHint">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InlayHint
    {
        /// <summary>
        /// Gets or sets the position that the inlay hint applies to.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("position")]
        public Position Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the label associated with this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("label")]
        public SumType<string, InlayHintLabelPart[]> Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the InlayHintKind associated with this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("kind")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TextEdits associated with this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("textEdits")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public TextEdit[]? TextEdits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tooltip of this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("tooltip")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<string, MarkupContent>? ToolTip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the padding before this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("paddingLeft")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PaddingLeft
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the padding after this inlay hint.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("paddingRight")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PaddingRight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data that should be preserved between a textDocument/inlayHint request and a inlayHint/resolve request.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
