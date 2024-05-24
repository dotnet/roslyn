// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/onTypeFormatting request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentOnTypeFormattingParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DocumentOnTypeFormattingParams : TextDocumentPositionParams
    {
        /// <summary>
        /// Gets or sets the character that was typed.
        /// </summary>
        [JsonPropertyName("ch")]
        public string Character
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="FormattingOptions"/> for the request.
        /// </summary>
        [JsonPropertyName("options")]
        public FormattingOptions Options
        {
            get;
            set;
        }
    }
}
