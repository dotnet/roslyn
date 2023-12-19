﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the parameters sent for the textDocument/willSave request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#willSaveTextDocumentParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class WillSaveTextDocumentParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentIdentifier"/> representing the document to be saved.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the reason that the text document was saved.
        /// </summary>
        [DataMember(Name = "reason")]
        public TextDocumentSaveReason Reason
        {
            get;
            set;
        }
    }
}
