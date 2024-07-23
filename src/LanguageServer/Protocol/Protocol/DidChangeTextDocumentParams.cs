// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents the parameter that is sent with textDocument/didChange message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeTextDocumentParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DidChangeTextDocumentParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the document that changed.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public VersionedTextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content changes.
        /// </summary>
        [JsonPropertyName("contentChanges")]
        public TextDocumentContentChangeEvent[] ContentChanges
        {
            get;
            set;
        }

        TextDocumentIdentifier ITextDocumentParams.TextDocument
        {
            get => this.TextDocument;
        }
    }
}
