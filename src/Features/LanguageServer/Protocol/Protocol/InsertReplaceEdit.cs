// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// A special text edit to provide an insert and a replace operation.
    /// 
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#insertReplaceEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InsertReplaceEdit
    {
        /// <summary>
        /// Gets or sets the string to be inserted.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("newText"), System.Text.Json.Serialization.JsonRequired]
        public string NewText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range range if the insert is requested
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insert"), System.Text.Json.Serialization.JsonRequired]
        public Range Insert
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range range if the replace is requested
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("replace"), System.Text.Json.Serialization.JsonRequired]
        public Range Replace
        {
            get;
            set;
        }
    }
}
