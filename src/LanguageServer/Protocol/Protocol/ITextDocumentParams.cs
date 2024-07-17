// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Interface to identify a text document.
    /// </summary>
    internal interface ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the value which identifies the document.
        /// </summary>
        public TextDocumentIdentifier TextDocument
        {
            get;
        }
    }
}
