// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents default range of InsertReplaceEdit for the entire completion list
    /// </summary>
    internal class InsertReplaceRange
    {
        /// <summary>
        /// Gets or sets the insert range.
        /// </summary>
        [JsonPropertyName("insert")]
        [JsonRequired]
        public Range Insert
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the replace edit range.
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
