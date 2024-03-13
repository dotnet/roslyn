// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;

    /// <summary>
    /// A class representing inlay hint label parts.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintLabelPart">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InlayHintLabelPart
    {
        /// <summary>
        /// Gets or sets the value associated with this label part.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tooltip of this label part.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("tooltip")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SumType<string, MarkupContent>? ToolTip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the location of this label part.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Location? Location
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the command of this label part.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("command")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }
    }
}
