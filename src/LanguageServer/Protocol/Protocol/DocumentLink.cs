// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the response of a textDocument/documentLink request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLink">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DocumentLink
    {
        /// <summary>
        /// Gets or sets the range the link applies to.
        /// </summary>
        [JsonPropertyName("range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the uri that the link points to.
        /// </summary>
        [JsonPropertyName("target")]
        [JsonConverter(typeof(DocumentUriConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Uri? Target
        {
            get;
            set;
        }
    }
}
