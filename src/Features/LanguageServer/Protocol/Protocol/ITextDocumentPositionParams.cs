// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Interface to identify a text document and a position inside that document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocumentPositionParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal interface ITextDocumentPositionParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the value which identifies the document.
        /// </summary>
        public new TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which indicates the position within the document.
        /// </summary>
        public Position Position
        {
            get;
            set;
        }
    }
}
