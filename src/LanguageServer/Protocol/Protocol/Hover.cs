// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the data returned by a textDocument/hover request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#hover">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class Hover
    {
        /// <summary>
        /// Gets or sets the content for the hover. Object can either be an array or a single object.
        /// If the object is an array the array can contain objects of type <see cref="MarkedString"/> and <see cref="string"/>.
        /// If the object is not an array it can be of type <see cref="MarkedString"/>, <see cref="string"/>, or <see cref="MarkupContent"/>.
        /// </summary>
        // This is nullable because in VS we allow null when VSInternalHover.RawContent is specified instead of Contents
        [JsonPropertyName("contents")]
        public SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>? Contents
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range over which the hover applies.
        /// </summary>
        [JsonPropertyName("range")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Range? Range
        {
            get;
            set;
        }
    }
}
