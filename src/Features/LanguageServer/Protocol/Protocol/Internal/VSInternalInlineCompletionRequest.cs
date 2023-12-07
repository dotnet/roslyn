// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// The request data for an inline completions request.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L24.
    /// </summary>
    internal class VSInternalInlineCompletionRequest : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the text document.
        /// </summary>
        [DataMember(Name = "_vs_textDocument")]
        [JsonProperty(Required = Required.Always)]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the position where inline completions are being requested.
        /// </summary>
        [DataMember(Name = "_vs_position")]
        [JsonProperty(Required = Required.Always)]
        public Position Position { get; set; }

        /// <summary>
        /// Gets or sets the context for the inline completions request.
        /// </summary>
        [DataMember(Name = "_vs_context")]
        [JsonProperty(Required = Required.Always)]
        public VSInternalInlineCompletionContext Context { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FormattingOptions"/> for the request.
        /// </summary>
        [DataMember(Name = "_vs_options")]
        public FormattingOptions Options { get; set; }
    }
}
