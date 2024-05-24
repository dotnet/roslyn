// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a completion list.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionList">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionList
    {
        /// <summary>
        /// Gets or sets a value indicating whether Items is the complete list of items or not.  If incomplete is true, then
        /// filtering should ask the server again for completion item.
        /// </summary>
        [JsonPropertyName("isIncomplete")]
        public bool IsIncomplete
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the list of completion items.
        /// </summary>
        [JsonPropertyName("items")]
        public CompletionItem[] Items
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion list item defaults.
        /// </summary>
        [JsonPropertyName("itemDefaults")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionListItemDefaults? ItemDefaults
        {
            get;
            set;
        }
    }
}