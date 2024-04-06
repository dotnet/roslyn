// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the parameter that is sent with a textDocument/didSave message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didSaveTextDocumentParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DidSaveTextDocumentParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentIdentifier"/> which represents the text document that was saved.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="string"/> which represents the content of the text document when it was saved.
        /// </summary>
        [DataMember(Name = "text")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Text
        {
            get;
            set;
        }
    }
}
