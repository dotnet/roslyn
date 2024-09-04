// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A special text edit to provide an insert and a replace operation.
    /// 
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#insertReplaceEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InsertReplaceEdit
    {
        /// <summary>
        /// Gets or sets the string to be inserted.
        /// </summary>
        [JsonPropertyName("newText")]
        [JsonRequired]
        public string NewText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range range if the insert is requested
        /// </summary>
        [JsonPropertyName("insert")]
        [JsonRequired]
        public Range Insert
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range range if the replace is requested
        /// </summary>
        [JsonPropertyName("replace")]
        [JsonRequired]
        public Range Replace
        {
            get;
            set;
        }
    }
}
