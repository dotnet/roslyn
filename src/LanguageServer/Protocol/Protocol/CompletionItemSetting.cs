// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents initialization setting for completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionItemSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether completion items can contain snippets.
        /// </summary>
        [JsonPropertyName("snippetSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SnippetSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports commit characters.
        /// </summary>
        [JsonPropertyName("commitCharactersSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool CommitCharactersSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content formats supported for documentation.
        /// </summary>
        [JsonPropertyName("documentationFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MarkupKind[]? DocumentationFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the deprecated property on a completion item.
        /// </summary>
        [JsonPropertyName("deprecatedSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeprecatedSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the preselect property on a completion item.
        /// </summary>
        [JsonPropertyName("preselectSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PreselectSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the tag property on a completion item.
        /// </summary>
        [JsonPropertyName("tagSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemTagSupportSetting? TagSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports insert replace edit.
        /// </summary>
        [JsonPropertyName("insertReplaceSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool InsertReplaceSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating which properties a client can resolve lazily on a completion item.
        /// </summary>
        [JsonPropertyName("resolveSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResolveSupportSetting? ResolveSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the `insertTextMode` property on   a completion item to override the whitespace handling mode as defined by the client.
        /// </summary>
        [JsonPropertyName("insertTextModeSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextModeSupportSetting? InsertTextModeSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports completion item label details.
        /// </summary>
        [JsonPropertyName("labelDetailsSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool LabelDetailsSupport
        {
            get;
            set;
        }
    }
}
