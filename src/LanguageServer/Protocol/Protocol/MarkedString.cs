// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing human readable text that should be rendered.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#markedString">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class MarkedString
    {
        /// <summary>
        /// Gets or sets the language of the code stored in <see cref="Value" />.
        /// </summary>
        [JsonPropertyName("language")]
        [JsonRequired]
        public string Language
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonRequired]
        public string Value
        {
            get;
            set;
        }
    }
}
