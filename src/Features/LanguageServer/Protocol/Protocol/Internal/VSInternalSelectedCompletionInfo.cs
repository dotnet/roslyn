// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Information about the selected completion item for <see cref="VSInternalInlineCompletionContext"/>.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L48.
    /// </summary>
    internal class VSInternalSelectedCompletionInfo
    {
        /// <summary>
        /// Gets or sets the range of the selected completion item.
        /// </summary>
        [DataMember(Name = "_vs_range")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Range Range { get; set; }

        /// <summary>
        /// Gets or sets the text of the selected completion item.
        /// </summary>
        [DataMember(Name = "_vs_text")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the completion item kind of the selected completion item.
        /// </summary>
        [DataMember(Name = "_vs_completionKind")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CompletionItemKind CompletionKind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the completion item is a snippet.
        /// </summary>
        [DataMember(Name = "_vs_isSnippetText")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsSnippetText { get; set; }
    }
}