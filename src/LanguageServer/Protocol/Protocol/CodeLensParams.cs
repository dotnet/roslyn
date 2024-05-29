// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent from the client to the server for a textDocument/codeLens request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLensParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeLensParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the document identifier to fetch code lens results for.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}
