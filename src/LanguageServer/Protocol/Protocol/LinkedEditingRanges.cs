// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the response of an LinkedEditingRanges response.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#linkedEditingRanges">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class LinkedEditingRanges
    {
        /// <summary>
        /// Gets or sets the ranges for the type rename.
        /// </summary>
        [JsonPropertyName("ranges")]
        public Range[] Ranges
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the word pattern for the type rename.
        /// </summary>
        [JsonPropertyName("wordPattern")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WordPattern
        {
            get;
            set;
        }
    }
}
