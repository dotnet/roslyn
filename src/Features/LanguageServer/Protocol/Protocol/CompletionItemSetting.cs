// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents initialization setting for completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionItemSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether completion items can contain snippets.
        /// </summary>
        [DataMember(Name = "snippetSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SnippetSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports commit characters.
        /// </summary>
        [DataMember(Name = "commitCharactersSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CommitCharactersSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content formats supported for documentation.
        /// </summary>
        [DataMember(Name = "documentationFormat")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public MarkupKind[]? DocumentationFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the deprecated property on a completion item.
        /// </summary>
        [DataMember(Name = "deprecatedSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DeprecatedSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the preselect property on a completion item.
        /// </summary>
        [DataMember(Name = "preselectSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PreselectSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the tag property on a completion item.
        /// </summary>
        [DataMember(Name = "tagSupport")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemTagSupportSetting? TagSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports insert replace edit.
        /// </summary>
        [DataMember(Name = "insertReplaceSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool InsertReplaceSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating which properties a client can resolve lazily on a completion item.
        /// </summary>
        [DataMember(Name = "resolveSupport")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ResolveSupportSetting? ResolveSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports the `insertTextMode` property on   a completion item to override the whitespace handling mode as defined by the client.
        /// </summary>
        [DataMember(Name = "insertTextModeSupport")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InsertTextModeSupportSetting? InsertTextModeSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the a value indicating whether the client supports completion item label details.
        /// </summary>
        [DataMember(Name = "labelDetailsSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool LabelDetailsSupport
        {
            get;
            set;
        }
    }
}
