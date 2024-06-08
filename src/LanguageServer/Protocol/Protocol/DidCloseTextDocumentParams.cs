﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents the parameter that is sent with textDocument/didClose message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didCloseTextDocumentParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DidCloseTextDocumentParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the text document identifier.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}
