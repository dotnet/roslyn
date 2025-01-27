// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client capabilities specific to completion.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class CompletionSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// The client supports the following <see cref="Protocol.CompletionItem"/> specific capabilities.
        /// </summary>
        [JsonPropertyName("completionItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemSetting? CompletionItem
        {
            get;
            set;
        }

        /// <summary>
        /// The client supports the following <see cref="Protocol.CompletionItemKind"/> values.
        /// </summary>
        [JsonPropertyName("completionItemKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemKindSetting? CompletionItemKind
        {
            get;
            set;
        }

        /// <summary>
        /// The client supports sending additional context information for
        /// a <c>textDocument/completion</c> request.
        /// </summary>
        [JsonPropertyName("contextSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ContextSupport
        {
            get;
            set;
        }

        /// <summary>
        /// The client's default insertion behavior when a completion item doesn't
        /// provide a value for the <see cref="CompletionItem.InsertTextMode"/> property.
        /// </summary>
        /// <remarks>Since 3.17</remarks>
        [JsonPropertyName("insertTextMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// The client supports the following <see cref="CompletionList"/> specific capabilities.
        /// </summary>
        /// <remarks>Since 3.17</remarks>
        [JsonPropertyName("completionList")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionListSetting? CompletionListSetting
        {
            get;
            set;
        }
    }
}
