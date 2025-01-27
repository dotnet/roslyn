// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A class representing inlay hint label parts.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintLabelPart">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    internal class InlayHintLabelPart
    {
        /// <summary>
        /// The value of this label part.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// The tooltip text when you hover over this label part.
        /// <para>
        /// Depending on the client capability <see cref="InlayHintSetting.ResolveSupport"/> clients
        /// might resolve this property late using the resolve request.
        /// </para>
        /// </summary>
        [JsonPropertyName("tooltip")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SumType<string, MarkupContent>? ToolTip
        {
            get;
            set;
        }

        /// <summary>
        /// An optional source code location that represents this label part.
        /// <para>
        /// The editor will use this location for the hover and for code navigation
        /// features. This part will become a clickable link that resolves to the
        /// definition of the symbol at the given location (not necessarily the
        /// location itself), it shows the hover that shows at the given location,
        /// and it shows a context menu with further code navigation commands.
        /// </para>
        /// <para>
        /// Depending on the client capability <see cref="InlayHintSetting.ResolveSupport"/> clients
        /// might resolve this property late using the resolve request.
        /// </para>
        /// </summary>
        [JsonPropertyName("location")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Location? Location
        {
            get;
            set;
        }

        /// <summary>
        /// An optional command for this label part.
        /// <para>
        /// Depending on the client capability <see cref="InlayHintSetting.ResolveSupport"/> clients
        /// might resolve this property late using the resolve request.
        /// </para>
        /// </summary>
        [JsonPropertyName("command")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }
    }
}
