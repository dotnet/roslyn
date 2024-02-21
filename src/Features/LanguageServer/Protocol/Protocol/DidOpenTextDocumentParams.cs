// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents the parameter that is sent with textDocument/didOpen message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didOpenTextDocumentParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DidOpenTextDocumentParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentItem"/> which represents the text document that was opened.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentItem TextDocument
        {
            get;
            set;
        }
    }
}
