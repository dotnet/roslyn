// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A class representing inlay hints that appear next to parameters or types.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHint">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    internal class InlayHint
    {
        /// <summary>
        /// The position of this hint.
        /// <para>
        /// If multiple hints have the same position, they will be shown in the order
        /// they appear in the response.
        /// </para>
        /// </summary>
        [JsonPropertyName("position")]
        [JsonRequired]
        public Position Position
        {
            get;
            set;
        }

        /// <summary>
        /// The label of this hint. A human readable string or an array of
        /// <see cref="InlayHintLabelPart"/> label parts.
        /// <para>
        /// Note that neither the string nor the label part can be empty.
        /// </para>
        /// </summary>
        [JsonPropertyName("label")]
        [JsonRequired]
        public SumType<string, InlayHintLabelPart[]> Label
        {
            get;
            set;
        }

        /// <summary>
        /// The kind of this hint. Can be omitted in which case the client
        /// should fall back to a reasonable default.
        /// </summary>
        [JsonPropertyName("kind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Optional text edits that are performed when accepting this inlay hint.
        /// <para>
        /// Note* that edits are expected to change the document so that the inlay
        /// hint(or its nearest variant) is now part of the document and the inlay
        /// hint itself is now obsolete.
        /// </para>
        /// <para>
        /// Depending on the client capability <see cref="InlayHintSetting.ResolveSupport"/> clients
        /// might resolve this property late using the resolve request.
        /// </para>
        /// </summary>
        [JsonPropertyName("textEdits")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextEdit[]? TextEdits
        {
            get;
            set;
        }

        /// <summary>
        /// The tooltip text when you hover over this item.
        /// <para>
        /// Depending on the client capability <see cref="InlayHintSetting.ResolveSupport"/> clients
        /// might resolve this property late using the resolve request.
        /// </para>
        /// </summary>
        [JsonPropertyName("tooltip")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<string, MarkupContent>? ToolTip
        {
            get;
            set;
        }

        /// <summary>
        /// Render padding before the hint.
        /// <para>
        /// Note: Padding should use the editor's background color, not the
        /// background color of the hint itself. That means padding can be used
        /// to visually align/separate an inlay hint.
        /// </para>
        /// </summary>
        [JsonPropertyName("paddingLeft")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PaddingLeft
        {
            get;
            set;
        }

        /// <summary>
        /// Render padding after the hint.
        /// <para>
        /// Note: Padding should use the editor's background color, not the
        /// background color of the hint itself.That means padding can be used
        /// to visually align/separate an inlay hint.
        /// </para>
        /// </summary>
        [JsonPropertyName("paddingRight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PaddingRight
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data that should be preserved between a
        /// <c>textDocument/inlayHint</c> request and a <c>inlayHint/resolve</c> request.
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
