// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which encapsulates a text document changed event.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentContentChangeEvent">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class TextDocumentContentChangeEvent
    {
        /// <summary>
        /// Gets or sets the range of the text that was changed.
        /// </summary>
        [JsonPropertyName("range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the length of the range that got replaced.
        /// </summary>
        [JsonPropertyName("rangeLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RangeLength
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the new text of the range/document.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text
        {
            get;
            set;
        }
    }
}
