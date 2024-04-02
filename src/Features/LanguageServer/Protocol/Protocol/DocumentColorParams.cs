// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/documentColor request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentColorParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentColorParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentIdentifier"/> to provide links for.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}
