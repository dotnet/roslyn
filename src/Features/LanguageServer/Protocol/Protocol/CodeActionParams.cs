// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent from the client to the server for the textDocument/codeAction request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeActionParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the document identifier indicating where the command was invoked.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range in the document for which the command was invoked.
        /// </summary>
        [JsonPropertyName("range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the additional diagnostic information about the code action context.
        /// </summary>
        [JsonPropertyName("context")]
        public CodeActionContext Context
        {
            get;
            set;
        }
    }
}
