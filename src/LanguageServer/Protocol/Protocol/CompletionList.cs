// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a completion list.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionList">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class CompletionList
    {
        /// <summary>
        /// This list is not complete. Further typing should result in recomputing this list.
        /// <para>
        /// Recomputed lists have all their items replaced (not appended) in the incomplete completion sessions.
        /// </para>
        /// </summary>
        [JsonPropertyName("isIncomplete")]
        [JsonRequired]
        public bool IsIncomplete
        {
            get;
            set;
        }

        /// <summary>
        /// Default values of <see cref="CompletionItem"/> properties for items
        /// that do not provide a value for those properties.
        /// <para>
        /// If a completion list specifies a default value and a completion item
        /// also specifies a corresponding value the one from the item is used.
        /// </para>
        /// <para>
        /// Servers are only allowed to return default values if the client
        /// signals support for this via the <see cref="CompletionListSetting.ItemDefaults"/>
        /// capability.
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("itemDefaults")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionListItemDefaults? ItemDefaults
        {
            get;
            set;
        }

        /// <summary>
        /// The completion items.
        /// </summary>
        [JsonPropertyName("items")]
        [JsonRequired]
        public CompletionItem[] Items
        {
            get;
            set;
        }
    }
}
