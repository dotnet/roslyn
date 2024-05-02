// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Response for an inline completions request.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L72.
    /// </summary>
    [DataContract]
    internal class VSInternalInlineCompletionList
    {
        /// <summary>
        /// Gets or sets the inline completion items.
        /// </summary>
        [DataMember(Name = "_vs_items")]
        [JsonProperty(Required = Required.Always)]
        public VSInternalInlineCompletionItem[] Items { get; set; }
    }
}
