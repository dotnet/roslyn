// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a text edit to a document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class TextEdit
    {
        /// <summary>
        /// Gets or sets the value which indicates the range of the text edit.
        /// </summary>
        [JsonPropertyName("range")]
        [JsonRequired]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the new text.
        /// </summary>
        [JsonPropertyName("newText")]
        public string NewText
        {
            get;
            set;
        }
    }
}
