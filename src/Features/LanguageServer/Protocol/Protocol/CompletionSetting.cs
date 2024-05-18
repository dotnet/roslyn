// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents initialization setting for completion.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets completion item setting.
        /// </summary>
        [JsonPropertyName("completionItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemSetting? CompletionItem
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets <see cref="Protocol.CompletionItemKind"/> specific settings.
        /// </summary>
        [JsonPropertyName("completionItemKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemKindSetting? CompletionItemKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports sending additional context.
        /// </summary>
        [JsonPropertyName("contextSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ContextSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating client's default when the completion item doesn't provide an `insertTextMode` property.
        /// </summary>
        [JsonPropertyName("insertTextMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports capabilities on the completion list.
        /// </summary>
        [JsonPropertyName("completionList")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionListSetting? CompletionListSetting
        {
            get;
            set;
        }
    }
}
