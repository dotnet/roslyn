// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents initialization setting for completion.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets completion item setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completionItem")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemSetting? CompletionItem
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets <see cref="Protocol.CompletionItemKind"/> specific settings.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completionItemKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemKindSetting? CompletionItemKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports sending additional context.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("contextSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ContextSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating client's default when the completion item doesn't provide an `insertTextMode` property.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insertTextMode")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports capabilities on the completion list.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completionList")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionListSetting? CompletionListSetting
        {
            get;
            set;
        }
    }
}
