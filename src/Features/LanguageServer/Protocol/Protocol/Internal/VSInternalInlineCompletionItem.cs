// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// A single inline completion item response.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L78.
    /// </summary>
    [DataContract]
    internal class VSInternalInlineCompletionItem
    {
        /// <summary>
        /// Gets or sets the text to replace the range with.
        /// </summary>
        [DataMember(Name = "_vs_text")]
        [JsonProperty(Required = Required.Always)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the range to replace.
        ///
        /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L94.
        /// </summary>
        [DataMember(Name = "_vs_range")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Range? Range { get; set; }

        /// <summary>
        /// Gets or sets the command that is executed after inserting this completion item.
        /// </summary>
        [DataMember(Name = "_vs_command")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Command? Command { get; set; }

        /// <summary>
        /// Gets or sets the format of the insert text.
        /// </summary>
        [DataMember(Name = "_vs_insertTextFormat")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public InsertTextFormat? TextFormat { get; set; } = InsertTextFormat.Plaintext;
    }
}
